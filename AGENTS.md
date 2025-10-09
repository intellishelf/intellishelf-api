# Repository Guidelines

## Project Structure & Module Organization
Intellishelf.Api hosts the ASP.NET Core entry point and feature modules in `Controllers`, `Services`, `Modules`, and `Contracts`. Domain logic lives in `Intellishelf.Domain`; persistence adapters in `Intellishelf.Data`; cross-cutting helpers in `Intellishelf.Common`. Config templates sit in `Intellishelf.Api/appsettings.*.json` and `secrets-example.json`. Tests use `Tests/Intellishelf.Unit.Tests` for isolated coverage and `Tests/Intellishelf.Integration.Tests` (backed by Testcontainers). Supporting scripts are in `scripts/`; `docker-compose.yml` remains available for manually running the API and backing services but is no longer required for automated tests.

## Build, Test, and Development Commands
- `dotnet restore Intellishelf.Api.sln` primes dependencies.
- `dotnet build Intellishelf.Api.sln -c Release` enforces a clean compile.
- `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Intellishelf.Api/Intellishelf.Api.csproj` serves the API at `http://localhost:8080/api`.
- `dotnet test Tests/Intellishelf.Unit.Tests/Intellishelf.Unit.Tests.csproj` covers isolated services.
- `dotnet test Tests/Intellishelf.Integration.Tests/Intellishelf.Integration.Tests.csproj` runs end-to-end scenarios. These tests spin up MongoDB and Azurite with Testcontainers—ensure Docker Desktop/daemon is running; no manual compose step is needed.
- `docker-compose up intellishelf-api` is optional for local manual testing; automated suites rely on Testcontainers instead.

## Coding Style & Naming Conventions
- Stick to .NET defaults: 4-space indentation, file-scoped namespaces, and braces on new lines for types/methods. Favour expression-bodied members when they keep intent obvious.
- Rely on primary constructors for services, controllers, records, and simple data carriers whenever dependency injection is straightforward. Fall back to explicit constructors only when additional setup logic is required.
- Use `PascalCase` for public types and members, `camelCase` for locals/parameters, and `_camelCase` for private fields. Mark fields `readonly` whenever viable.
- Keep single-responsibility files; place API contracts under `Intellishelf.Api/Contracts` and cross-cutting helpers in the shared projects (`Intellishelf.Common`, etc.).
- Mirror existing nullability annotations, prefer guard clauses over deep nesting, and favour immutability (records or `init` properties) for DTOs.
- Run `dotnet format` before pushing or raising a PR to enforce analyzers and style rules.

## Error Handling
Service and DAO layers return `TryResult<T>` from `Intellishelf.Common.TryResult` to model success and rich errors. Prefer propagating the `Error` payload rather than throwing; controllers convert errors into `ProblemDetails` responses with explicit status codes so API clients get actionable messages.

## Authentication & Authorization
Web clients authenticate via Google OAuth and receive an ASP.NET cookie ticket; retain cookie middleware when adjusting the pipeline. Mobile clients use the JWT auth scheme with bearer tokens signed by our configured key. Keep both schemes registered and validate new endpoints against the correct policy before merging.

## Testing Guidelines
Tests use xUnit with Moq; name classes `*Tests` (e.g., `BooksControllerTests`). Cover new service logic with unit tests and add integration tests whenever contracts or persistence change. Integration suites boot dependencies with Testcontainers—avoid hard-coding credentials and keep fixtures deterministic. If Docker isn’t available, skip integration execution and report the limitation.

## Integration Test Infrastructure
- `Tests/Intellishelf.Integration.Tests/Infra` hosts shared fixtures and helpers. `MongoDbFixture`/`AzuriteFixture` use Testcontainers to provision fresh instances per collection.
- `DefaultTestUsers` defines the canonical authenticated user used across integration specs and the `TestAuthHandler`. When new tests need an authenticated context, prefer seeding via `MongoDbFixture.SeedDefaultUserAsync()` and reusing the baked login request/claims rather than duplicating literals.
- `TestWebApplicationFactory` swaps in the test auth scheme and injects container-provided configuration for database and storage endpoints. Keep new tests within the `[Collection("Integration Tests")]` context so fixtures share container lifetimes.

## Commit & Pull Request Guidelines
Follow the imperative commit style (`Add …`, `Fix …`). PRs should link issues, summarize functional changes, call out new env vars, and note how you tested. Attach screenshots or HTTP samples for user-facing updates and request reviews from module owners.

## Configuration & Secrets
Never commit real secrets. Update `secrets-example.json` when adding keys and load sensitive values via user secrets, environment variables, or Azure Key Vault. Override `appsettings.Development.json` locally without hardcoding credentials.
