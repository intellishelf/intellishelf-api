---

description: "Implementation tasks for ISBN Book Search and Quick Add feature"
---

# Tasks: ISBN Book Search and Quick Add

**Input**: Design documents from `/specs/001-isbn-book-search/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: Per constitution, integration tests are REQUIRED for all API contracts and persistence changes. Unit tests are optional and only included for isolated validation logic (ISBN format validation).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Path Conventions

- **API Layer**: `src/Intellishelf.Api/` (Controllers, Contracts, Configuration)
- **Domain Layer**: `src/Intellishelf.Domain/` (Business logic, Services)
- **Data Layer**: `src/Intellishelf.Data/` (Repositories, DAOs)
- **Common**: `src/Intellishelf.Common/` (TryResult, shared utilities)
- **Integration Tests**: `tests/Intellishelf.Integration.Tests/`
- **Unit Tests**: `tests/Intellishelf.Unit.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and NuGet package installation

- [X] T001 Install Google Books API NuGet package: `dotnet add src/Intellishelf.Api/Intellishelf.Api.csproj package Google.Apis.Books.v1`
- [X] T002 [P] Verify MongoDB.Driver package exists in Intellishelf.Data project
- [X] T003 [P] Verify Azure.Storage.Blobs package exists in Intellishelf.Domain project (updated: found in Domain, not Api)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Add BookSource enum to `src/Intellishelf.Domain/Books/BookSource.cs` (moved from BookEntity to avoid circular dependency)
- [X] T005 Add Source property to BookEntity in `src/Intellishelf.Data/Books/Entities/BookEntity.cs`
- [X] T006 [P] Add error codes (InvalidIsbn, DuplicateBook, ExternalApiFailure) to `src/Intellishelf.Domain/Books/Errors/BookErrorCodes.cs`
- [X] T007 Create BookMetadata DTO in `src/Intellishelf.Domain/Books/BookMetadata.cs`
- [X] T008 [P] Create IIsbnLookupService interface in `src/Intellishelf.Domain/Books/IIsbnLookupService.cs`
- [X] T009 [P] Add FindByIsbnAsync method signature to IBookDao in `src/Intellishelf.Domain/Books/DataAccess/IBookDao.cs`
- [X] T010 [P] Add TryAddBookByIsbnAsync method signature to IBookService in `src/Intellishelf.Domain/Books/Services/IBookService.cs`
- [X] T011 Implement FindByIsbnAsync in BookDao with dual ISBN query in `src/Intellishelf.Data/Books/DataAccess/BookDao.cs`
- [X] T012 [P] Create MongoDB indexes (userId+isbn13 unique, userId+isbn10) via EnsureIndexesAsync() in BookDao
- [X] T013 [P] Create Google Books API configuration section in `src/Intellishelf.Api/appsettings.json` and `appsettings.Development.json`
- [X] T014 [P] Add Google Books API key to `src/Intellishelf.Api/secrets-example.json`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Quick Add Book by Valid ISBN (Priority: P1) 🎯 MVP

**Goal**: Users can add books to their collection by entering a valid ISBN-10 or ISBN-13, with book metadata automatically retrieved from external APIs

**Independent Test**: Provide valid ISBN (e.g., "978-0134685991"), verify book details are retrieved from Google Books, and confirm book appears in user's collection with complete metadata

### Integration Tests for User Story 1 (REQUIRED) ⚠️

> **NOTE: Per constitution, integration tests are REQUIRED for API contracts and persistence.**
> Write integration tests FIRST, ensure they FAIL before implementation.

- [X] T015 [P] [US1] Integration test: AddBookByIsbn_ValidIsbn13_ReturnsCreated in `tests/Intellishelf.Integration.Tests/BooksTests.cs`
- [X] T016 [P] [US1] Integration test: AddBookByIsbn_ValidIsbn10_ReturnsCreated in `tests/Intellishelf.Integration.Tests/BooksTests.cs`
- [X] T017 [P] [US1] Integration test: AddBookByIsbn_Duplicate_ReturnsConflict in `tests/Intellishelf.Integration.Tests/BooksTests.cs`
- [X] T018 [P] [US1] Integration test: AddBookByIsbn_GoogleFails_FallsBackToAmazon in `tests/Intellishelf.Integration.Tests/BooksTests.cs` (placeholder for Phase 5)

### Unit Tests for User Story 1 (OPTIONAL - Isolated Logic Only)

- [ ] T019 [P] [US1] Unit test: NormalizeIsbn_RemovesHyphensAndSpaces in `tests/Intellishelf.Unit.Tests/Books/BookServiceTests.cs` (OPTIONAL - helpers are private methods)
- [ ] T020 [P] [US1] Unit test: IsValidIsbnFormat_ValidIsbn10_ReturnsTrue in `tests/Intellishelf.Unit.Tests/Books/BookServiceTests.cs` (OPTIONAL - helpers are private methods)
- [ ] T021 [P] [US1] Unit test: IsValidIsbnFormat_ValidIsbn13_ReturnsTrue in `tests/Intellishelf.Unit.Tests/Books/BookServiceTests.cs` (OPTIONAL - helpers are private methods)

### Implementation for User Story 1

**Follow layered architecture: Domain → interfaces, Data → implementations, Api → controllers**

- [X] T022 [P] [US1] Create AddBookByIsbnRequest in `src/Intellishelf.Api/Contracts/Books/AddBookByIsbnRequest.cs`
- [X] T023 [P] [US1] No AddBookByIsbnResponse needed - returns Book directly (standard pattern per existing endpoints)
- [X] T024 [P] [US1] No mapper needed - ISBN passed directly to service (minimal contract)
- [X] T025 [US1] Implement TryAddBookByIsbnAsync in BookService with ISBN validation helpers (NormalizeIsbn, IsValidIsbnFormat) in `src/Intellishelf.Domain/Books/Services/BookService.cs`
- [X] T026 [US1] Implement IsbnLookupService with Google Books API client in `src/Intellishelf.Domain/Books/Services/IsbnLookupService.cs`
- [X] T027 [US1] Add POST /api/books/isbn endpoint in BooksController with error mapping in `src/Intellishelf.Api/Controllers/BooksController.cs`
- [X] T028 [US1] Register IIsbnLookupService → IsbnLookupService in DI container in `src/Intellishelf.Api/Modules/BooksModule.cs` + add Google.Apis.Books.v1 to Domain project
- [ ] T029 [US1] Verify integration tests pass: Run all 4 integration tests for US1 (requires Google Books API key configuration)

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Handle Invalid or Not Found ISBN (Priority: P1)

**Goal**: Users receive clear, actionable error messages when entering invalid ISBNs or ISBNs not found in external catalogs

**Independent Test**: Provide invalid ISBN formats (e.g., "123-ABC") and valid ISBNs not in external catalogs, verify appropriate HTTP status codes and ProblemDetails responses

### Integration Tests for User Story 2 (REQUIRED) ⚠️

- [ ] T030 [P] [US2] Integration test: AddBookByIsbn_InvalidFormat_ReturnsBadRequest in `tests/Intellishelf.Integration.Tests/BooksTests.cs`
- [ ] T031 [P] [US2] Integration test: AddBookByIsbn_NotFound_ReturnsNotFound in `tests/Intellishelf.Integration.Tests/BooksTests.cs`
- [ ] T032 [P] [US2] Integration test: AddBookByIsbn_ExternalApiUnavailable_ReturnsServiceUnavailable in `tests/Intellishelf.Integration.Tests/BooksTests.cs`

### Implementation for User Story 2

- [ ] T033 [US2] Add error handling in BookService.AddBookByIsbnAsync for invalid ISBN format (already covered in T025, verify error returns)
- [ ] T034 [US2] Add error handling in IsbnLookupService for BookNotFound from all APIs in `src/Intellishelf.Domain/Books/IsbnLookupService.cs`
- [ ] T035 [US2] Configure HttpClient timeout (5 seconds per external API) in IsbnLookupService constructor in `src/Intellishelf.Domain/Books/IsbnLookupService.cs`
- [ ] T036 [US2] Add error handling in IsbnLookupService for API timeouts and network failures in `src/Intellishelf.Domain/Books/IsbnLookupService.cs`
- [ ] T037 [US2] Add ProblemDetails mapping in BooksController for all error codes (400, 404, 409, 503) in `src/Intellishelf.Api/Controllers/BooksController.cs`
- [ ] T038 [US2] Verify integration tests pass: Run all 3 integration tests for US2

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently with proper error handling

---

## Phase 5: User Story 3 - Search Multiple Sources with Priority (Priority: P2)

**Goal**: System queries multiple book sources (Google Books primary, Amazon fallback) to maximize success rate and data quality

**Independent Test**: Monitor which API was used for book retrieval, verify fallback behavior when Google Books fails, and confirm metadata merging when both APIs return data

### Integration Tests for User Story 3 (OPTIONAL - Covered by US1 T018)

> Already covered by T018: AddBookByIsbn_GoogleFails_FallsBackToAmazon

### Implementation for User Story 3

- [ ] T039 [P] [US3] Create custom Amazon Product Advertising API client with AWS Signature v4 in `src/Intellishelf.Domain/Books/AmazonProductClient.cs`
- [ ] T040 [US3] Add Amazon API fallback logic to IsbnLookupService.LookupByIsbnAsync in `src/Intellishelf.Domain/Books/IsbnLookupService.cs`
- [ ] T041 [US3] Add logging for which API source was used (Google/Amazon) in `src/Intellishelf.Domain/Books/IsbnLookupService.cs`
- [ ] T042 [P] [US3] Add Amazon API configuration to `src/Intellishelf.Api/appsettings.json` and `secrets-example.json`
- [ ] T043 [US3] Implement metadata merging logic (prefer most complete fields from both APIs) in `src/Intellishelf.Domain/Books/IsbnLookupService.cs`
- [ ] T044 [US3] Verify fallback test passes: Run T018 integration test

**Checkpoint**: All user stories should now be independently functional with multi-source support

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T045 [P] Update quickstart.md with actual curl/HTTP examples from integration tests
- [ ] T046 [P] Add XML documentation comments to all public service methods
- [ ] T047 [P] Performance optimization: Add response caching headers for GET endpoints
- [ ] T048 Verify all integration tests pass: `dotnet test tests/Intellishelf.Integration.Tests/Intellishelf.Integration.Tests.csproj`
- [ ] T049 [P] Verify all unit tests pass: `dotnet test tests/Intellishelf.Unit.Tests/Intellishelf.Unit.Tests.csproj`
- [ ] T050 Run quickstart.md validation: Follow developer guide end-to-end
- [ ] T051 [P] Code review: Verify all constitutional principles pass (Modern .NET Stack, ProblemDetails, Result<T>, Integration-First Testing, Layered Architecture, Anemic Domain Models)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) - Core MVP functionality
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) - Error handling (P1 priority, should ship with US1)
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2) - Amazon fallback (P2 priority, can ship after MVP)
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Extends US1 error handling but independently testable
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Adds Amazon fallback to IsbnLookupService created in US1

### Within Each User Story

- Integration tests MUST be written FIRST and FAIL before implementation
- Unit tests (if included) can be written in parallel with integration tests
- DTOs and interfaces (marked [P]) before services
- Services before controllers
- DI registration after implementation complete
- Verify tests pass before moving to next story

### Parallel Opportunities

**Setup Phase (Phase 1)**:
- T002 and T003 can run in parallel (different packages)

**Foundational Phase (Phase 2)**:
- T006, T007, T008, T009, T010 can run in parallel (different files, interface definitions)
- T013 and T014 can run in parallel (different config files)

**User Story 1 (Phase 3)**:
- All integration tests (T015-T018) can run in parallel
- All unit tests (T019-T021) can run in parallel
- DTOs and Mappers (T022-T024) can run in parallel

**User Story 2 (Phase 4)**:
- All integration tests (T030-T032) can run in parallel

**User Story 3 (Phase 5)**:
- T038 and T041 can run in parallel (client implementation + configuration)

**Polish Phase (Phase 6)**:
- T044, T045, T046 can run in parallel (documentation, comments, optimization)
- T047 and T048 can run in parallel (different test projects)

---

## Parallel Example: User Story 1

```bash
# Launch all integration tests for User Story 1 together:
Task T015: "Integration test: AddBookByIsbn_ValidIsbn13_ReturnsCreated"
Task T016: "Integration test: AddBookByIsbn_ValidIsbn10_ReturnsCreated"
Task T017: "Integration test: AddBookByIsbn_Duplicate_ReturnsConflict"
Task T018: "Integration test: AddBookByIsbn_GoogleFails_FallsBackToAmazon"

# Launch all unit tests for User Story 1 together:
Task T019: "Unit test: NormalizeIsbn_RemovesHyphensAndSpaces"
Task T020: "Unit test: IsValidIsbnFormat_ValidIsbn10_ReturnsTrue"
Task T021: "Unit test: IsValidIsbnFormat_ValidIsbn13_ReturnsTrue"

# Launch all DTOs for User Story 1 together:
Task T022: "Create AddBookByIsbnRequest"
Task T023: "Create AddBookByIsbnResponse"
Task T024: "Create BookMapper.ToAddBookByIsbnResponse"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (3 tasks)
2. Complete Phase 2: Foundational (11 tasks - CRITICAL)
3. Complete Phase 3: User Story 1 (15 tasks)
4. Complete Phase 4: User Story 2 (5 tasks)
5. **STOP and VALIDATE**: Test both US1 and US2 independently
6. Deploy/demo MVP with Google Books only

**MVP Scope**: Single ISBN addition with validation and error handling (no Amazon fallback)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Core functionality works
3. Add User Story 2 → Test independently → Error handling complete → **Deploy MVP** ✅
4. Add User Story 3 → Test independently → Amazon fallback added → Deploy enhanced version
5. Polish phase → Documentation and optimization → Final release

### Parallel Team Strategy

With multiple developers:

1. **Team completes Setup + Foundational together** (Phases 1-2)
2. **Once Foundational is done**:
   - Developer A: User Story 1 tests (T015-T021)
   - Developer B: User Story 1 implementation (T022-T028)
   - Developer C: User Story 2 preparation (read plan, prepare test data)
3. **After US1 complete**:
   - Developer A: User Story 2 tests + implementation (T030-T037)
   - Developer B: User Story 3 Amazon client (T038, T041)
   - Developer C: Polish phase documentation (T044, T045)
4. Stories integrate and test independently

---

## Task Count Summary

- **Phase 1 (Setup)**: 3 tasks
- **Phase 2 (Foundational)**: 11 tasks ⚠️ **CRITICAL PATH**
- **Phase 3 (User Story 1 - P1)**: 15 tasks 🎯 **MVP CORE**
- **Phase 4 (User Story 2 - P1)**: 9 tasks 🎯 **MVP ERROR HANDLING**
- **Phase 5 (User Story 3 - P2)**: 6 tasks (Optional for MVP)
- **Phase 6 (Polish)**: 7 tasks
- **TOTAL**: 51 tasks

**MVP Scope** (US1 + US2): 38 tasks (Setup + Foundational + US1 + US2)
**Full Feature**: 51 tasks (all phases)

**Parallel Opportunities**: 23 tasks marked [P] can run in parallel within their phases

---

## Notes

- [P] tasks = different files, no dependencies within phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Integration tests are REQUIRED per constitution (marked ⚠️)
- Unit tests are optional and only for isolated validation logic
- Verify integration tests fail before implementing
- Commit after each logical group of tasks
- Stop at any checkpoint to validate story independently
- MVP ships with US1 + US2 (Google Books only, no Amazon fallback)
- US3 adds Amazon fallback for production resilience
