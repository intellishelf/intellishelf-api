<!--
Sync Impact Report:
- Version change: 1.1.0 → 1.2.0
- Amendment: Added Anemic Domain Models principle (MINOR version bump)
- Principles modified:
  1. Modern .NET Stack (unchanged)
  2. ProblemDetails Error Handling (unchanged)
  3. TryResult Pattern (unchanged)
  4. Integration-First Testing (unchanged)
  5. Pragmatic Testing Strategy (unchanged)
  6. Layered Architecture (unchanged)
  7. Anemic Domain Models (new)
- Added sections: Principle VII with entity/service separation guidance
- Removed sections: None
- Templates requiring updates:
  ✅ plan-template.md - Constitution Check updated with anemic model validation
  ✅ spec-template.md - Already aligned
  ✅ tasks-template.md - Already aligned with project structure
- Follow-up TODOs: Update existing ISBN plan to move Isbn value object logic to service layer
-->

# Intellishelf API Constitution

## Core Principles

### I. Modern .NET Stack

**MUST** use modern .NET (currently .NET 8+) with latest stable language features and framework capabilities.
**MUST** leverage ASP.NET Core for all API endpoints with file-scoped namespaces and primary constructors for dependency injection.
**MUST** follow .NET naming conventions: `PascalCase` for public types/members, `camelCase` for locals/parameters, `_camelCase` for private fields.
**MUST** mark fields `readonly` whenever viable and favor immutability using records or `init` properties for DTOs.

**Rationale**: Modern .NET provides performance, security, and developer productivity improvements. Consistent conventions ensure maintainability across a startup-sized codebase where team velocity matters.

### II. ProblemDetails Error Handling

**MUST** return errors to API clients using `ProblemDetails` responses with explicit HTTP status codes.
**MUST** provide actionable error messages that clients can surface to users or handle programmatically.
**MUST NOT** expose internal stack traces or sensitive implementation details in production error responses.

**Rationale**: Standardized error responses using RFC 7807 ProblemDetails enable consistent client-side error handling across web and mobile apps. Explicit status codes prevent ambiguity and improve debugging.

### III. TryResult Pattern

**MUST** use `TryResult<T>` from `Intellishelf.Common.TryResult` for communication between service and data access layers.
**MUST** propagate rich `Error` payloads rather than throwing exceptions for expected failure scenarios.
**MUST** convert `TryResult` errors to `ProblemDetails` at the controller layer with appropriate HTTP status codes.
**MUST NOT** use exceptions for flow control in business logic.

**Rationale**: The TryResult pattern makes failure paths explicit, reduces exception overhead, and provides rich error context for better diagnostics. Controllers act as the translation boundary between domain errors and HTTP responses.

### IV. Integration-First Testing

**MUST** cover all API contracts and persistence changes with integration tests using Testcontainers.
**MUST** use `Tests/Intellishelf.Integration.Tests` with xUnit collection fixtures for test isolation.
**MUST** ensure integration tests boot dependencies (MongoDB, Azurite) automatically without manual docker-compose steps.
**MUST** keep integration test fixtures deterministic and avoid hard-coded credentials.

**Rationale**: Integration tests validate end-to-end behavior and catch contract/persistence regressions that unit tests miss. Testcontainers enable reproducible test environments without infrastructure setup burden, critical for startup velocity.

### V. Pragmatic Testing Strategy

**MUST** prioritize integration tests for majority of feature coverage.
**SHOULD** write unit tests only for isolated logic that benefits from fast feedback (e.g., validation, parsing, calculations).
**MUST NOT** aim for arbitrary coverage percentages; focus on risk areas and contract stability.
**MAY** skip unit tests for simple CRUD operations, thin controllers, or DTOs covered by integration tests.

**Rationale**: Startup context demands efficient use of testing effort. Integration tests provide maximum confidence per line of test code. Unit tests target specific high-value scenarios (image validation, business rules) where isolation clarifies intent.

### VI. Layered Architecture

**MUST** organize code into distinct layers with clear dependency direction: `Api → Domain ← Data`, with `Common` as a shared foundation.

**MUST** keep `Intellishelf.Domain` independent of infrastructure concerns; it MUST depend only on `Intellishelf.Common`.

**MUST** define interfaces in `Intellishelf.Domain` for data access and external services (named `I{Feature}Dao`, `I{Feature}Service`).

**MUST** implement data access interfaces in `Intellishelf.Data` with concrete implementations (named `{Feature}Dao`).

**MUST** place domain services, models (DTOs), and domain-specific errors in `Intellishelf.Domain/{Feature}/`.

**MUST** separate interfaces and implementations into distinct files (e.g., `IBookService.cs` and `BookService.cs`).

**MUST** suffix all async methods with `Async` (e.g., `GetBookAsync`, `TryAddBookAsync`).

**MUST NOT** introduce circular dependencies between projects or features.

**Rationale**: Layered architecture with dependency inversion enables independent domain logic testing, clear boundaries for persistence concerns, and flexibility to swap infrastructure. Separating interfaces from implementations supports mockability and testability. The Domain project's independence from Data prevents tight coupling to MongoDB or Azure-specific details, critical for potential future migrations or multi-tenancy scenarios.

### VII. Anemic Domain Models

**MUST** keep entities (database models in `Intellishelf.Data/{Feature}/Entities/`) as pure data containers with no business logic.

**MUST** use simple properties with init-only setters (`init`) or getters/setters where appropriate.

**MUST** place all validation, business rules, and domain logic in service classes (`Intellishelf.Domain/{Feature}/Services/`).

**MUST NOT** create "value objects" or "domain entities" with embedded behavior, methods, or validation logic in the entity classes themselves.

**MUST** use DTOs or records in `Intellishelf.Domain/{Feature}/Models/` for passing data between layers, not for encapsulating behavior.

**MAY** use static factory methods or builder patterns in service classes (not entities) when complex object construction is needed.

**Rationale**: This codebase follows a pragmatic, transaction-script style architecture rather than Domain-Driven Design (DDD). Entities are simple MongoDB documents mapped via the driver's serialization. Business logic lives in stateless service classes that orchestrate operations across entities and external APIs. This approach:
- Reduces complexity for a startup-sized team (simpler mental model than rich domain objects)
- Aligns with MongoDB's document-oriented nature (entities mirror database structure)
- Keeps testable logic in services, not spread across multiple entity classes
- Avoids impedance mismatch between OOP domain models and document storage
- Enables straightforward serialization/deserialization without custom converters

Example of **correct** approach:
```csharp
// Entity (Data layer) - Pure data container
public class BookEntity : EntityBase
{
    public required string Title { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    // ... other properties, no methods
}

// Service (Domain layer) - Contains logic
public class BookService : IBookService
{
    public Result<BookEntity> ValidateAndCreateBook(string isbn, ...)
    {
        // Validation logic here
        if (!IsValidIsbnFormat(isbn))
            return new Error(BookErrorCodes.InvalidIsbn, "...");
        
        // Business logic here
        var normalizedIsbn = NormalizeIsbn(isbn);
        
        return new BookEntity
        {
            Isbn13 = normalizedIsbn,
            // ... other fields
        };
    }
    
    private bool IsValidIsbnFormat(string isbn) { /* ... */ }
    private string NormalizeIsbn(string isbn) { /* ... */ }
}
```

Example of **incorrect** approach (do NOT do this):
```csharp
// ❌ BAD - Entity with behavior
public record Isbn
{
    public string Value { get; }
    
    private Isbn(string value) => Value = value;
    
    public static Result<Isbn> Create(string input)
    {
        if (!IsValid(input))
            return new Error(...);
        return new Isbn(Normalize(input));
    }
    
    private static bool IsValid(string input) { /* ... */ }
    // ❌ Logic in entity/value object
}
```

## Technology Stack Requirements

**Language/Runtime**: .NET 8+ with C# latest stable version
**API Framework**: ASP.NET Core with minimal APIs or controllers
**Database**: MongoDB with official .NET driver
**Storage**: Azure Blob Storage via Azure.Storage.Blobs SDK
**Authentication**: Google OAuth for web (cookie-based), JWT bearer tokens for mobile
**Testing**: xUnit with Moq for mocking, Testcontainers for integration test infrastructure
**CI/CD**: Must support `dotnet restore`, `dotnet build`, `dotnet test` workflows
**Code Quality**: Must run `dotnet format` before PR submission

## Architectural Layering

The solution follows a layered architecture with explicit dependency rules:

**Dependency Flow**: `Intellishelf.Api` → `Intellishelf.Domain` ← `Intellishelf.Data`  
**Shared Foundation**: All projects may depend on `Intellishelf.Common`

### Layer Responsibilities

**Intellishelf.Common**:
- Shared utilities (`TryResult<T>`, error types)
- Cross-cutting concerns available to all layers
- No business logic or infrastructure dependencies

**Intellishelf.Domain**:
- Domain services (business logic orchestration)
- Domain models (DTOs, request/response objects)
- Domain-specific errors
- Interface definitions for data access (`I{Feature}Dao`) and external services (`I{Feature}Service`)
- Organized by feature: `{Feature}/Services/`, `{Feature}/Models/`, `{Feature}/DataAccess/`, `{Feature}/Errors/`
- Dependencies: ONLY `Intellishelf.Common` and framework packages (Azure SDK, OpenAI, etc.)

**Intellishelf.Data**:
- Data access implementations (`{Feature}Dao` implementing `I{Feature}Dao`)
- Database entities (MongoDB documents)
- Entity-to-model mappers
- Organized by feature: `{Feature}/DataAccess/`, `{Feature}/Entities/`, `{Feature}/Mappers/`
- Dependencies: `Intellishelf.Domain`, `Intellishelf.Common`, MongoDB.Driver

**Intellishelf.Api**:
- Controllers (HTTP request handling, authentication, authorization)
- API contracts (request/response DTOs specific to HTTP layer)
- Configuration classes
- Dependency injection modules
- TryResult-to-ProblemDetails conversion
- Dependencies: `Intellishelf.Domain`, `Intellishelf.Data`, `Intellishelf.Common`

### Architecture Rules

- Domain logic MUST NOT reference Data or Api projects
- Data implementations MUST implement Domain-defined interfaces
- Controllers MUST convert TryResult errors to ProblemDetails responses
- Feature organization MUST be consistent across all layers
- Interfaces and implementations MUST reside in separate files

## Development Workflow

**Project Structure**: Multi-project solution with `Intellishelf.Api` (web layer), `Intellishelf.Domain` (business logic), `Intellishelf.Data` (persistence), `Intellishelf.Common` (shared utilities).

**Branching**: Feature branches follow imperative naming (`add-feature`, `fix-bug`). PRs link issues and summarize changes.

**Quality Gates**:
- All integration tests MUST pass before merge
- Unit tests (where present) MUST pass
- `dotnet format` MUST be run to enforce style
- New endpoints MUST have corresponding integration tests
- Breaking changes to contracts MUST be documented in PR

**Configuration Management**:
- MUST NOT commit secrets
- MUST update `secrets-example.json` when adding new configuration keys
- MUST load sensitive values via user secrets, environment variables, or Azure Key Vault

**Code Review Requirements**:
- PRs require approval from module owner
- HTTP samples or screenshots for user-facing changes
- Explicit callout of new environment variables or configuration

## Governance

This constitution supersedes all ad-hoc practices. All feature specifications, plans, and task lists MUST align with these principles.

**Amendment Process**:
- Constitution changes require documentation of rationale and impact analysis
- Version MUST increment per semantic versioning:
  - **MAJOR**: Backward incompatible governance changes or principle removals
  - **MINOR**: New principles or materially expanded guidance
  - **PATCH**: Clarifications, wording fixes, non-semantic refinements
- Amendments MUST include Sync Impact Report covering affected templates and documents
- All dependent templates and guidance files MUST be updated for consistency

**Compliance**:
- All PRs and code reviews MUST verify alignment with constitution principles
- Violations require explicit justification in PR description
- Complexity additions beyond startup scope MUST be challenged against YAGNI principle
- Runtime development guidance lives in `AGENTS.md` and `.github/custom-instructions.md`

**Version**: 1.2.0 | **Ratified**: 2025-10-29 | **Last Amended**: 2025-10-30
