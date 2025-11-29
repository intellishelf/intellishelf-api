
This file provides guidance to LLM agent when working with code in this repository.

## Build & Test Commands

```bash
# Restore dependencies
dotnet restore Intellishelf.Api.sln

# Build the solution
dotnet build Intellishelf.Api.sln -c Release

# Run the API locally (requires MongoDB and Azure Storage configuration)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Intellishelf.Api/Intellishelf.Api.csproj

# Run unit tests
dotnet test tests/Intellishelf.Unit.Tests/Intellishelf.Unit.Tests.csproj

# Run integration tests (requires Docker running for Testcontainers)
dotnet test tests/Intellishelf.Integration.Tests/Intellishelf.Integration.Tests.csproj

# Run a specific test class
dotnet test --filter "FullyQualifiedName~BooksTests"

# Format code
dotnet format
```

## Architecture Overview

This is a .NET 9 Web API following **Clean Architecture** with 4 main layers:

```
src/
├── Intellishelf.Api/              Web API, Controllers, Modules (DI configuration)
├── Intellishelf.Domain/           Business logic, Service interfaces, Domain models
├── Intellishelf.Data/             Data access implementations, MongoDB repositories
└── Intellishelf.Common/           Shared utilities (TryResult pattern)
```

**Dependency flow:** Api → Domain → Data ← Common

**Key principle:** Domain defines interfaces (IBookDao, IBookService), Data implements them. Controllers depend only on Domain abstractions.

## TryResult Pattern (Critical for Error Handling)

**Location:** `src/Intellishelf.Common/TryResult/TryResult.cs`

Railway-oriented programming instead of exceptions. Service/DAO methods return `TryResult<T>` with two states: Success (has Value) or Error (has Error).

**Usage:** Check `result.IsSuccess`, return `HandleErrorResponse(result.Error)` or `Ok(result.Value)`. Can return values directly (implicit conversion), errors via `new Error(code, message)`, or void via `TryResult.Success()`.

**Error Codes:** Format `{Feature}.{ErrorType}` (e.g., `Books.NotFound`). Define as `public const string` in `Domain/{Feature}/Errors/{Feature}ErrorCodes.cs`.

## Error to HTTP Status Mapping

**Location:** `src/Intellishelf.Api/Controllers/ApiControllerBase.cs`

Map error codes to HTTP status in `MapErrorToStatusCode()` switch. Add new error codes to switch (404 for NotFound, 401 for Unauthorized, 409 for AlreadyExists, 400 for InvalidInput, 502 for external service errors, 500 default).

## Module System (Dependency Injection)

**Location:** `src/Intellishelf.Api/Modules/`

DI organized into modules per feature: `DbModule`, `UsersModule`, `BooksModule`, `AzureModule`, `AiModule`. Each module has static `Register()` method called in `Program.cs`.

**Lifecycle:** Singleton (mappers, validators, processors - stateless), Transient (DAOs, services - per injection), Scoped (per-request state, rare), HttpClient (for HTTP services).

## Layer Communication Flow

HTTP Request → Auth Middleware (JWT/Cookie/Google) → Controller (extract UserId) → Service (business logic orchestration) → DAO (MongoDB queries) → Mapper (Entity→Domain) → TryResult → Controller (HandleErrorResponse/Ok) → ProblemDetails/JSON.

**Key:** Services orchestrate cross-cutting concerns, DAOs focus on data access only.

## Integration Tests with Testcontainers

**Location:** `tests/Intellishelf.Integration.Tests/`

Use real MongoDB and Azure Storage containers (not mocks). Fixtures (`MongoDbFixture`, `AzuriteFixture`) spin up containers via Testcontainers. Tests use `[Collection("Integration Tests")]` to share fixtures. `TestWebApplicationFactory` swaps dependencies and uses `TestAuthHandler` for auth bypass.

**Requirements:** Docker running. Tests isolated per collection. Seed data via fixtures (`SeedDefaultUserAsync()`, `ClearBooksAsync()`).

## Data Access Patterns

**Pattern:** Interface in Domain, implementation in Data. DAOs inject `IMongoDatabase`, get collection via `database.GetCollection<TEntity>(CollectionName)`.

**MongoDB:** Use `Builders<T>.Filter/Update/Sort` for queries. `ObjectId` for user IDs, string for entity IDs. Entities inherit `EntityBase` with `[BsonId]`. Return `TryResult` from all DAO methods. Map entities to domain models via injected mappers.

## Authentication Architecture

**Location:** `src/Intellishelf.Api/Modules/UsersModule.cs`

Three schemes routed by policy: JWT Bearer (mobile, `Authorization` header), Cookie (web sessions), Google OAuth (external provider). Policy selector checks Authorization header to route between JWT/Cookie.

All endpoints require `[Authorize]` by default (via `ApiControllerBase`). Access user ID via `CurrentUserId` property from claims.

## AI Integration (OpenAI)

**Location:** `src/Intellishelf.Domain/Ai/Services/AiService.cs`

Parse OCR text to structured book data using OpenAI with JSON schema validation (strict mode, temp 0.0). Supports mock mode for testing. Endpoint: `POST /books/parse-text`.

## Important File Paths

**Core abstractions:**
- `src/Intellishelf.Common/TryResult/TryResult.cs` - Error handling pattern
- `src/Intellishelf.Api/Controllers/ApiControllerBase.cs` - Base controller with error mapping
- `src/Intellishelf.Data/EntityBase.cs` - MongoDB entity base class

**Example implementations (reference these for patterns):**
- `src/Intellishelf.Domain/Books/Services/BookService.cs` - Service layer with file cleanup
- `src/Intellishelf.Data/Books/DataAccess/BookDao.cs` - MongoDB DAO implementation
- `src/Intellishelf.Domain/Users/Services/AuthService.cs` - JWT + OAuth auth logic

**Configuration:**
- `src/Intellishelf.Api/Program.cs` - Application startup
- `src/Intellishelf.Api/Modules/` - DI configuration per feature
- `src/Intellishelf.Api/appsettings.json` - App configuration (do not commit secrets)

**Test infrastructure:**
- `tests/Intellishelf.Integration.Tests/Infra/TestWebApplicationFactory.cs` - Test host factory
- `tests/Intellishelf.Integration.Tests/Infra/Fixtures/` - Testcontainers fixtures

## Naming Conventions

**Files:** `[Domain]Controller.cs`, `I[Domain]Service.cs`, `[Domain]Service.cs`, `I[Entity]Dao.cs`, `[Entity]Dao.cs`, `[Entity]Entity.cs`, `[Domain]ErrorCodes.cs`, `[Feature]Module.cs`, `[Service]Config.cs`, `[Domain]Helper.cs`

**Methods:** `Try[Action]Async` (returns TryResult), `[Action]Async` (async), `Map[Target]` (mappers)

**Variables:** `result`/`[action]Result`, `_[entityPlural]Collection`, `_[feature]Config`, `userIdObject = ObjectId.Parse(userId)`

**Feature Structure:**
```
Domain/{Feature}/Services|Models|DataAccess|Errors|Helpers|Config
Data/{Feature}/DataAccess|Entities|Mappers
Api/Controllers|Contracts/{Feature}|Mappers
```

## Validation Patterns

**File Validation:** Validators return `TryResult` (see `ImageFileValidator.cs`). Check file size, content type, extensions. Return error codes like `FileErrorCodes.FileTooLarge`, `FileErrorCodes.InvalidFileType`.

**Domain Validation:** Use static helper classes (e.g., `IsbnHelper.IsValidIsbn()`, `IsbnHelper.NormalizeIsbn()`). Return errors via `TryResult`.

**Self-Validating Query Parameters:** Use property setters to enforce constraints (e.g., auto-clamp PageSize to max 100).

## Configuration Management

**Config Classes:** Define `SectionName` constant, use `required` for mandatory values, nullable for optional (see `DatabaseConfig`, `AiConfig`, `AuthConfig`).

**Module Pattern:** Load config via `GetSection()`, bind with `Configure<T>()`, access via `IOptions<T>` injection. Modules register services in `Program.cs`.

## File Processing Pipeline

**Three-Stage:** Validate (check size/type) → Process (resize to max 1000x1000, optimize) → Upload (to Azure Blob Storage). Controllers orchestrate all stages, return errors at each step.

## Background Services

Inherit from `BackgroundService`, use `IServiceProvider` to create scopes for transient dependencies. Register with `AddHostedService<T>()`. Example: `RefreshTokenCleanupService` runs every 24 hours.

## Security Patterns

**Cookies:** MUST use `HttpOnly = true`, `Secure = true`, `SameSite = Lax` for CSRF protection.

**Password Hashing:** HMAC-SHA512 with random salt (see `AuthHelper.CreatePasswordHash()`, `AuthHelper.VerifyPasswordHash()`).

**Authorization:** All controllers inherit `ApiControllerBase` with `[Authorize]` attribute. Use `CurrentUserId` property from base class. Use `[AllowAnonymous]` for public endpoints.

## Code Quality Standards

**Pre-commit:** Run `dotnet format`. All tests must pass before merge.

**Code Review:** Verify TryResult usage, error codes mapped, integration tests added, no secrets committed, layer dependencies correct, async naming, proper DI lifecycle.

## Extension Guide: Adding a New Feature

**Quick Checklist:**
1. Domain model + request/response records in `Domain/{Feature}/Models/`
2. Error codes in `Domain/{Feature}/Errors/` + map in `ApiControllerBase.MapErrorToStatusCode()`
3. DAO interface in `Domain/{Feature}/DataAccess/`
4. Entity (inherit `EntityBase`, define `CollectionName`) in `Data/{Feature}/Entities/`
5. Entity mapper (interface + impl) in `Data/{Feature}/Mappers/`
6. DAO implementation (use `Builders<T>.Filter`, return `TryResult`) in `Data/{Feature}/DataAccess/`
7. Service interface + implementation in `Domain/{Feature}/Services/`
8. API contracts in `Api/Contracts/{Feature}/`
9. API mapper in `Api/Mappers/`
10. Controller (inherit `ApiControllerBase`, use `CurrentUserId`) in `Api/Controllers/`
11. Module (register with correct lifetimes) in `Api/Modules/` + add to `Program.cs`
12. Integration tests in `tests/Intellishelf.Integration.Tests/Features/`

**Reference existing features** (Books, Users, Chat) for implementation patterns.
