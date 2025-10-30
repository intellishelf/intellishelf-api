# Implementation Plan: ISBN Book Search and Quick Add

**Branch**: `001-isbn-book-search` | **Date**: 2025-10-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-isbn-book-search/spec.md`

## Summary

Enable users to quickly add books to their digital bookshelf by ISBN (10 or 13-digit) without manual data entry. The system validates the ISBN, queries external book metadata APIs (Google Books primary, Amazon fallback), downloads and stores cover images in Azure Blob Storage, and adds the book to the user's MongoDB collection with both ISBN formats stored for search optimization.

**Technical Approach**: Build a layered .NET API following constitutional principles with simplified ISBN format validation (no check digit calculation), external API integration layer with fallback strategy (Google Books primary, custom Amazon HTTP client with AWS Signature v4), Azure Blob Storage integration for cover images, and MongoDB persistence with dual ISBN indexing. Uses existing BookEntity structure with added Source enum field.

## Technical Context

**Language/Version**: .NET 9.0 with C# 13 (latest stable per existing codebase)  
**Primary Dependencies**: ASP.NET Core, MongoDB.Driver (existing), Azure.Storage.Blobs (existing), Google.Apis.Books.v1 (new), System.Net.Http (built-in for custom Amazon client)  
**Storage**: MongoDB for book metadata, Azure Blob Storage for cover images  
**Testing**: xUnit with Moq, Testcontainers for integration tests (MongoDB, Azurite)  
**Target Platform**: Azure App Service (Linux containers via Docker)
**Project Type**: Multi-project .NET solution (API + Domain + Data + Common)  
**Performance Goals**: <5 seconds ISBN lookup end-to-end including external API calls, <200ms API response after data cached  
**Constraints**: Startup-friendly pragmatic approach, minimize external API costs, optimize for search performance via dual ISBN storage  
**Scale/Scope**: Support 100 concurrent ISBN searches, small to medium user base (thousands of users, millions of books)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Modern .NET Stack**: Feature uses .NET 9.0, ASP.NET Core, follows naming conventions (PascalCase, file-scoped namespaces, primary constructors)
- [x] **ProblemDetails Error Handling**: API errors return ProblemDetails with explicit status codes (400 Bad Request for invalid ISBN, 404 Not Found for book not found, 409 Conflict for duplicates, 503 Service Unavailable for API failures)
- [x] **TryResult Pattern**: BookService and BookDao use Result<T> pattern with BookErrorCodes for ISBN validation, external API calls, duplicate detection, and persistence operations
- [x] **Integration-First Testing**: Integration tests for POST /api/books/isbn endpoint covering valid/invalid ISBN, duplicate detection, external API fallback, cover image download
- [x] **Pragmatic Testing**: Unit tests only for ISBN format validation helper methods in service layer (no value objects)
- [x] **Layered Architecture**: IBookService and IBookDao interfaces in Domain, implementations in Domain/Data respectively, no circular dependencies
- [x] **Anemic Domain Models**: BookEntity is pure data container with no logic; all ISBN validation and business rules in BookService

*All constitution checks pass. No complexity tracking violations.*

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths specific to this feature.
-->

```text
# .NET Multi-Project Solution (Intellishelf API)
src/
├── Intellishelf.Api/          # API layer (Controllers, Contracts, Configuration)
│   ├── Controllers/
│   │   └── BooksController.cs         # Existing: Add POST /api/books/isbn endpoint
│   ├── Contracts/
│   │   └── Books/
│   │       ├── AddBookByIsbnRequest.cs  # New: { Isbn: string }
│   │       └── AddBookByIsbnResponse.cs # New: { BookId, Title, Authors[], etc. }
│   └── Modules/
│       └── BooksModule.cs             # Existing: Add IIsbnLookupService registration
├── Intellishelf.Domain/       # Business logic and services
│   ├── Books/
│   │   ├── BookEntity.cs              # Existing: Verify ISBN-10/ISBN-13 fields exist
│   │   │                              # Add Source enum (Google/Amazon/Manual)
│   │   ├── IBookService.cs            # Existing: Add AddBookByIsbnAsync(userId, isbn)
│   │   ├── BookService.cs             # Existing: Add ISBN validation, lookup orchestration
│   │   │                              # ISBN validation helpers as private methods
│   │   ├── IIsbnLookupService.cs      # New: External API integration interface
│   │   ├── IsbnLookupService.cs       # New: Google Books → Amazon fallback logic
│   │   └── BookMetadata.cs            # New: DTO from external APIs
│   └── Files/
│       └── IImageStorageService.cs    # Existing: Verify DownloadAndStoreAsync method
├── Intellishelf.Data/        # Data access layer
│   └── Books/
│       ├── IBookDao.cs                # Existing: Verify FindByIsbnAsync signature
│       └── BookDao.cs                 # Existing: Verify dual ISBN (10/13) search
└── Intellishelf.Common/      # Shared utilities (TryResult, etc.)
    └── TryResult/
        └── BookErrorCodes.cs          # Existing: Add InvalidIsbn, DuplicateBook, ExternalApiFailure

tests/
├── Intellishelf.Integration.Tests/  # Integration tests (required for API/persistence)
│   ├── BooksTests.cs                  # Add 6 test methods:
│   │                                  # - AddBookByIsbn_ValidIsbn10_ReturnsCreated
│   │                                  # - AddBookByIsbn_ValidIsbn13_ReturnsCreated
│   │                                  # - AddBookByIsbn_InvalidFormat_ReturnsBadRequest
│   │                                  # - AddBookByIsbn_NotFound_ReturnsNotFound
│   │                                  # - AddBookByIsbn_Duplicate_ReturnsConflict
│   │                                  # - AddBookByIsbn_GoogleFails_FallsBackToAmazon
│   └── Infra/
│       └── DefaultTestUsers.cs        # Existing: Use default authenticated user
└── Intellishelf.Unit.Tests/         # Unit tests (optional, for isolated logic)
    └── Domain/
        └── Books/
            └── BookServiceTests.cs    # New: Test ISBN validation helpers in service
```

**Structure Decision**: 
- Use existing `Books/` feature folders across all layers (Domain, Data, Api)
- **Extend existing BookEntity** with `Source` enum field (Google/Amazon/Manual)
- Add new `IIsbnLookupService` interface in Domain for external API abstraction
- **ISBN validation as private helper methods** in `BookService` (no value objects, following anemic model principle)
- **Custom Amazon API client**: Build HTTP client with AWS Signature v4 (no unofficial packages)
- Leverage existing `IImageStorageService` for cover image download/storage
- Extend existing `BooksController` with new endpoint (no new controller needed)
- Add 3 new error codes to `BookErrorCodes` for ISBN-specific failures

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*No violations. All constitutional principles followed.*

---

## Phase Completion Status

### ✅ Phase 0: Research (COMPLETE)
**Output**: `research.md` (6,245 lines)

Researched and documented:
- Simplified ISBN-10 and ISBN-13 format validation (no check digit calculation)
- ISBN-10 to ISBN-13 conversion logic (prepend 978, placeholder check digit)
- Google Books API v1 integration (search by ISBN, response schema, NuGet package)
- **Custom Amazon Product Advertising API client** (AWS Signature v4 implementation)
- External API fallback strategy (Google primary → Amazon fallback)
- Azure Blob Storage integration for cover images (download and persist)
- MongoDB dual ISBN indexing strategy (compound indexes on userId+isbn10, userId+isbn13)
- Performance optimization strategies (HTTP timeouts, parallel operations)
- NuGet package dependencies (Google.Apis.Books.v1 only, no Amazon package)

**Key Decisions**:
- Simplified ISBN validation (format only, no check digit math)
- Google Books API as primary source (simpler auth, free tier)
- **Custom Amazon API client** (no unofficial packages, manual AWS Signature v4)
- Dual ISBN storage (ISBN-10 nullable, ISBN-13 nullable) matching existing entity
- **BookSource enum** for Source field (Google/Amazon/Manual)
- Azure Blob Storage for cover image persistence (stable URLs)
- HTTP timeout 5 seconds per external API call (max 10s total)

### ✅ Phase 1: Design (COMPLETE)

#### Data Model (`data-model.md`)
**Output**: 4,821 lines

Designed:
- **BookEntity extended** with `Source` enum field (BookSource.Google/Amazon/Manual)
- **ISBN validation logic** as private helper methods in `BookService` (anemic entities, logic in services)
- `BookMetadata` record (intermediate DTO from external APIs)
- `IIsbnLookupService` interface (external API abstraction)
- `AddBookByIsbnRequest` and `AddBookByIsbnResponse` contracts
- MongoDB indexes (unique userId+isbn13, non-unique userId+isbn10)
- **BookErrorCodes** with 3 new constants (InvalidIsbn, DuplicateBook, ExternalApiFailure)
- Aligned with existing entity structure (CoverImageUrl, PublicationDate, Authors as string[])

**Constitution Re-Check**:
- ✅ Modern .NET: Uses records, value objects, file-scoped namespaces, init-only properties
- ✅ ProblemDetails: All errors map to explicit status codes (400/404/409/503)
- ✅ Result Pattern: All service/DAO methods return Result<T>, errors use BookErrorCodes constants
- ✅ Integration-First: 6 integration tests planned (valid/invalid/duplicate/fallback)
- ✅ Pragmatic Testing: Unit tests only for ISBN validation helpers in service (no value object tests)
- ✅ Layered Architecture: IIsbnLookupService in Domain, no circular dependencies
- ✅ **Anemic Domain Models**: BookEntity is pure data container; all validation in BookService methods

#### API Contracts (`contracts/openapi.yaml`)
**Output**: OpenAPI 3.0 specification (298 lines)

Defined:
- `POST /api/books/isbn` endpoint
- Request/response schemas (AddBookByIsbnRequest, AddBookByIsbnResponse)
- Error responses (400/401/404/409/503 with ProblemDetails)
- Authentication schemes (cookieAuth for web, bearerAuth for mobile)
- Example requests and responses for all scenarios

#### Quickstart Guide (`quickstart.md`)
**Output**: Developer setup and testing guide (522 lines)

Documented:
- Prerequisites (Google Books API key, MongoDB, Azure Storage)
- Environment setup (user secrets, appsettings configuration)
- Database setup (MongoDB indexes, seed data)
- API testing examples (curl commands, HTTP file samples)
- Integration test execution (Testcontainers)
- Troubleshooting guide (API key errors, MongoDB connection, Docker issues)

---

## Next Steps

### Agent Context Update
Run the agent context update script to document new dependencies and external API integrations:

```bash
.specify/scripts/bash/update-agent-context.sh copilot
```

This will update `AGENTS.md` with:
- Google Books API integration details
- Amazon Product Advertising API (optional)
- ISBN validation patterns
- Dual ISBN storage strategy

### Task Generation (Phase 2)
Generate implementation tasks with:

```bash
/speckit.tasks
```

This will create `tasks.md` with:
- Granular implementation tasks (4-8 hours each)
- Test scenarios mapped to tasks
- Dependency ordering (data model → services → controller → tests)
- Acceptance criteria linked to spec requirements

### Constitution Post-Design Review
All constitutional checks **PASS** after Phase 1 design:
- ✅ No new projects added (reused existing Books/ folders)
- ✅ No circular dependencies (Domain interfaces, Data implementations)
- ✅ TryResult<T> used consistently across all layers
- ✅ ProblemDetails error responses with explicit HTTP status codes
- ✅ Integration tests cover all API scenarios
- ✅ Unit tests limited to ISBN validation logic (isolated)

**No complexity tracking issues identified.**
