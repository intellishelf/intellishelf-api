# Code Review Guide for LLM Reviewers

Quick-reference for reviewing PRs in this codebase. For full architecture details see `/AGENTS.md`.

Always start review with a greeting like "Hi, engineer" etc

## Style Fingerprint

- .NET 9, C# with `nullable enable`, `ImplicitUsings`
- Primary constructors for DI injection (no field assignments)
- `required` + `init` for immutability; `null!` for sentinel defaults
- Arrow-body (`=>`) for single-expression methods and mappers
- No comments unless logic is non-obvious; no XML docs on internal code
- Allman braces for blocks, but inline lambdas are fine
- Private fields: `_camelCase`; collections: `_booksCollection`
- Async everywhere for I/O; suffix `Async` on method names
- `string[]?` over `List<string>?` for optional arrays in models
- Commits are terse (often just "upd", "feat: ..." one-liners)

## What to Flag

### Architecture violations
- Domain project referencing Data or Api (dependency flows Api -> Domain -> Data <- Common)
- DAO doing business logic (should be pure data access + mapping)
- Service calling HttpContext or HTTP-specific APIs
- Controller doing business logic beyond map-call-respond

### TryResult misuse
- Not checking `IsSuccess` before accessing `.Value`
- Throwing exceptions instead of returning `Error`
- Missing error code in `ApiControllerBase.MapErrorToStatusCode()` switch
- Error code format not matching `{Feature}.{ErrorType}` convention

### DI lifecycle bugs
- Stateful service registered as Singleton
- Mapper or validator registered as Transient (waste; should be Singleton)
- DAO or Service registered as Singleton (holds MongoDB collection refs that may go stale)
- Missing registration in the feature Module

### MongoDB gotchas
- Forgetting `UserId` filter (multi-tenant data leak)
- Using `string` instead of `ObjectId` for user ID in entity filters
- Not checking `MatchedCount`/`ModifiedCount` on update/delete operations
- Missing index considerations for new query patterns

### Security
- Endpoint missing `[Authorize]` (all endpoints require auth unless explicitly `[AllowAnonymous]`)
- Not using `CurrentUserId` from base class (rolling own claim extraction)
- Cookies without `HttpOnly`, `Secure`, `SameSite`
- Secrets in appsettings or committed config

### Naming drift
- Controller not named `{Domain}Controller`
- Service method not prefixed `Try` when returning `TryResult`
- Entity missing `CollectionName` constant
- Error codes class not named `{Feature}ErrorCodes`

### Testing gaps
- New endpoint without integration test
- Test not using Given/When/Then naming: `Given[State]_When[Action]_Then[Result]`
- Test not clearing data in `InitializeAsync` (leaking state between tests)
- Not using `[Collection("Integration Tests")]` to share fixtures

## Quick Checklist for New Features

- [ ] Domain model with `required`/`init` properties
- [ ] Error codes defined + mapped to HTTP status
- [ ] DAO interface in Domain, implementation in Data
- [ ] Entity inherits `EntityBase`, has `CollectionName`
- [ ] Mapper is pure (no side effects, no async)
- [ ] Module registers all types with correct lifetimes
- [ ] Module's `Register()` called in `Program.cs`
- [ ] Controller inherits `ApiControllerBase`, uses `CurrentUserId`
- [ ] All DAO queries include `UserId` filter
- [ ] Integration test covers happy path + key error paths

## Patterns to Preserve

**Controller flow:** Extract userId -> call service -> check IsSuccess -> return Ok/HandleErrorResponse

**Service flow:** Validate -> fetch/check -> business logic -> call DAO -> return result/error

**DAO flow:** Parse userId to ObjectId -> build filter -> query -> null-check -> map entity to domain model -> return

**Mapper flow:** Pure function, arrow body, handle nulls with `??` and `[]` defaults

## Red Flags in Diffs

- `throw new Exception` in service/DAO code (use TryResult)
- `async void` anywhere
- `.Result` or `.Wait()` on tasks (sync-over-async)
- `services.AddScoped` without clear per-request justification
- New `using` statements in Domain project pointing to Data/Api namespaces
- Hard-coded connection strings or API keys
