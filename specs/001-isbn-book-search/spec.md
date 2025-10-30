# Feature Specification: ISBN Book Search and Quick Add

**Feature Branch**: `001-isbn-book-search`  
**Created**: 2025-10-29  
**Status**: Draft  
**Input**: User description: "Add feature to allow users to quickly add books by ISBN (10 or 13). API integrates with Amazon and Google search. If book is found, API adds book into storage and returns the book data. If not found, return error."

## Clarifications

### Session 2025-10-29

- Q: Should subscription tier limits and batch ISBN import be included in this feature? → A: No, subscription system not implemented yet; batch import deferred to future release. Focus on single ISBN addition only.
- Q: Which external API should be the primary source for ISBN lookups? → A: Google Books primary, Amazon fallback (cost-effective: Google Books has generous free tier)
- Q: How should book cover images be handled? → A: Download and store locally in Azure Blob Storage (ensures availability, better control)
- Q: Should duplicate ISBN detection be per-user or system-wide? → A: Per-user duplicate check (same user cannot add same ISBN twice, but different users can have the same book)
- Q: Which ISBN format should be stored in the database? → A: Store both ISBN-10 and ISBN-13 in separate fields without dashes (used for search)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Quick Add Book by Valid ISBN (Priority: P1)

A user has a book with a visible ISBN code and wants to add it to their digital bookshelf without manually entering all book details. They provide the ISBN, and the system automatically retrieves book information from external sources and adds it to their collection.

**Why this priority**: This is the core value proposition of the feature - eliminating manual data entry. Without this, the feature provides no value.

**Independent Test**: Can be fully tested by providing a valid ISBN-10 or ISBN-13, verifying that book details are retrieved from external APIs (Amazon/Google), and confirming the book appears in the user's collection with complete metadata.

**Acceptance Scenarios**:

1. **Given** a user is authenticated and has an empty bookshelf, **When** they provide a valid ISBN-13 (e.g., "978-0134685991"), **Then** the system retrieves book details (title, author, publisher, cover image, publication date, description), adds the book to their collection, and returns the complete book data with HTTP 201 Created.

2. **Given** a user is authenticated, **When** they provide a valid ISBN-10 (e.g., "0134685997"), **Then** the system normalizes it, converts to ISBN-13 format, retrieves book details from external sources, stores both ISBN-10 and ISBN-13 in the book record, and returns the complete book data with HTTP 201 Created.

3. **Given** a user already has a book in their collection, **When** they attempt to add the same ISBN again, **Then** the system returns HTTP 409 Conflict with a ProblemDetails response indicating "Book already exists in your collection" and includes the existing book's ID.

4. **Given** Google Books API fails but Amazon API succeeds, **When** a user provides a valid ISBN, **Then** the system uses Amazon data as a fallback and successfully adds the book with HTTP 201 Created.

---

### User Story 2 - Handle Invalid or Not Found ISBN (Priority: P1)

A user enters an ISBN that doesn't match any known book, or provides malformed input. The system validates the ISBN format and searches external sources, providing clear feedback when a book cannot be found.

**Why this priority**: Error handling is critical for user trust and prevents bad data in the system. This must ship with the core feature.

**Independent Test**: Can be fully tested by providing invalid ISBNs (wrong format, invalid check digit) and valid ISBNs that don't exist in external catalogs, verifying appropriate error messages are returned.

**Acceptance Scenarios**:

1. **Given** a user is authenticated, **When** they provide an ISBN with invalid format (e.g., "123-ABC"), **Then** the system returns HTTP 400 Bad Request with ProblemDetails explaining "Invalid ISBN format. Please provide a valid 10 or 13-digit ISBN."

2. **Given** a user provides a correctly formatted ISBN that exists in neither Google Books nor Amazon, **When** the system searches both sources, **Then** it returns HTTP 404 Not Found with ProblemDetails stating "Book not found in our catalog. Please check the ISBN or add the book manually."

3. **Given** both Google Books and Amazon APIs are unavailable or timeout, **When** a user provides a valid ISBN, **Then** the system returns HTTP 503 Service Unavailable with ProblemDetails indicating "Book search temporarily unavailable. Please try again later."

---

### User Story 3 - Search Multiple Sources with Priority (Priority: P2)

The system intelligently queries multiple book information sources (Amazon, Google Books) with a fallback strategy to maximize the chance of finding accurate book metadata. Users benefit from the most comprehensive and reliable data available.

**Why this priority**: Improves data quality and success rate but the feature still works with a single source. Can be enhanced after initial release.

**Independent Test**: Can be tested by monitoring which API was used for book retrieval and verifying fallback behavior when primary source fails or returns incomplete data.

**Acceptance Scenarios**:

1. **Given** Google Books API returns results, **When** a user searches for an ISBN, **Then** the system uses Google Books data and logs that Google Books was the primary source.

2. **Given** Google Books API returns minimal data (only title and author) but Amazon API returns rich metadata (description, categories, cover image), **When** the system retrieves book information, **Then** it merges data from both sources, preferring the most complete fields, and stores the enriched book data.

3. **Given** Google Books API is unavailable but Amazon API responds, **When** a user searches for an ISBN, **Then** the system uses Amazon data as fallback and successfully adds the book with HTTP 201 Created.

---

### Edge Cases

- **What happens when ISBN format is ambiguous (hyphens, spaces)?** System MUST normalize input by removing hyphens, spaces, and converting to standard format before validation. → Expected HTTP status: 200/201 if valid after normalization, 400 if still invalid.

- **What happens when external APIs return conflicting information for the same ISBN?** System MUST prefer Google Books data (primary source), but merge fields from Amazon if Google Books data is incomplete. → Expected HTTP status: 201, with metadata indicating which source(s) were used.

- **What happens when book cover image URL is broken or inaccessible?** System MUST attempt to download the cover image; if download fails, still add the book without the cover and log the issue. User can upload cover manually later. → Expected HTTP status: 201, with cover field set to null.

- **What happens when ISBN is valid but the book is restricted/unavailable in certain regions?** System MUST attempt to retrieve data from alternative sources. If all fail, return 404. → Expected HTTP status: 404 Not Found with ProblemDetails indicating regional availability issue if detectable.

- **What happens when the same ISBN is associated with different editions (hardcover, paperback, international)?** System MUST store the specific edition returned by the API and allow users to have multiple editions of the same book. → Expected HTTP status: 201, treating each edition as a distinct book entry.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept both ISBN-10 and ISBN-13 formats as input.

- **FR-002**: System MUST validate ISBN format (10 or 13 digits, correct prefix 978/979 for ISBN-13) before querying external sources.

- **FR-003**: System MUST normalize ISBN input by removing hyphens, spaces, and other non-numeric characters (except 'X' in ISBN-10 check digit). System MUST store both ISBN-10 and ISBN-13 formats in separate fields without dashes for search optimization.

- **FR-004**: System MUST query Google Books API as the primary source and Amazon Product Advertising API as fallback for book metadata when a valid ISBN is provided.

- **FR-005**: System MUST implement a fallback strategy: if Google Books API fails or times out, attempt Amazon API before returning an error.

- **FR-006**: System MUST retrieve and store at minimum: title, author(s), publisher, publication date, description. System MUST download book cover images from external API URLs and store them in Azure Blob Storage, associating the blob URL with the book record.

- **FR-007**: System MUST detect duplicate books in the authenticated user's collection by checking both ISBN-10 and ISBN-13 fields before adding and return HTTP 409 Conflict if the user already has that ISBN. Different users may have the same ISBN in their respective collections.

- **FR-008**: System MUST associate the added book with the authenticated user's ID.

- **FR-009**: System MUST return complete book data in the response after successful addition (same format as GET book endpoint).

- **FR-010**: System MUST log which external API source was used for each successful book retrieval for analytics and debugging.

- **FR-011**: System MUST handle API timeouts gracefully with a maximum wait time of 5 seconds per external source.

- **FR-012**: System MUST implement idempotency: submitting the same ISBN multiple times in quick succession should not create duplicate entries.

- **FR-013**: System MUST return ProblemDetails-formatted errors for all failure scenarios (invalid ISBN, not found, API unavailable, duplicate).

### Key Entities

- **Book**: Represents a book in the user's digital collection. Key attributes: ISBN-10 (10-digit format without dashes, may be null for newer books), ISBN-13 (13-digit format without dashes, required), title, author(s), publisher, publication date, cover image blob URL (stored in Azure Blob Storage), description, date added, user ID (owner), source (which API provided the data).

- **ISBN Search Request**: Represents a user's request to add a book by ISBN. Key attributes: ISBN, user ID, timestamp.

- **External Book Metadata**: Temporary representation of book data retrieved from Amazon or Google Books APIs. Mapped to internal Book entity before storage.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can add a book to their collection in under 5 seconds from ISBN entry to confirmation (including external API lookup).

- **SC-002**: System successfully retrieves and adds books for 95% of valid ISBNs from currently published books (less than 10 years old).

- **SC-003**: System provides clear, actionable error messages for all failure scenarios (invalid format, not found, duplicate, API unavailable) with 100% coverage.

- **SC-004**: 90% of ISBN searches return results from the primary configured source (measure fallback usage rate).

- **SC-005**: System handles at least 100 concurrent ISBN search requests without degradation in response time.

- **SC-006**: User satisfaction: 85% of users successfully add their first book via ISBN on the first attempt without errors.

## Assumptions

- **A-001**: Amazon Product Advertising API and Google Books API will be available with acceptable uptime (>99% SLA assumed).

- **A-002**: Both external APIs provide sufficient book metadata (title, author, publisher, cover) for the majority of published books.

- **A-003**: API rate limits from Amazon and Google are sufficient for expected user load.

- **A-003**: Users have network connectivity and the system has network access to download cover images from external API URLs.

- **A-005**: ISBN-10 format, while older, is still commonly used and should be supported alongside ISBN-13.

- **A-006**: External APIs return data in a consistent format that can be reliably parsed.

## Dependencies

- **D-001**: Access to Amazon Product Advertising API requires API credentials and agreement to Amazon's terms of service.

- **D-002**: Google Books API access requires API key and adherence to usage limits.

- **D-003**: Existing user authentication system must be in place to associate books with user accounts.

- **D-004**: Existing book storage schema and CRUD operations must support the book entity structure.

- **D-005**: Azure Blob Storage must be configured and accessible for storing downloaded book cover images.

## Out of Scope

- **OS-001**: Batch ISBN import - deferred to future release. Focus is on single ISBN addition only.

- **OS-002**: Subscription tier limits for ISBN searches - subscription system not yet implemented.

- **OS-003**: Manual book entry form (already exists as separate feature).

- **OS-004**: Editing book details after ISBN-based addition (covered by existing book update functionality).

- **OS-005**: Barcode scanning via mobile camera (handled by mobile app, which sends extracted ISBN to this API).

- **OS-006**: Offline ISBN lookup or local ISBN database (always requires external API calls).

- **OS-007**: Integration with additional book metadata sources beyond Amazon and Google Books.

- **OS-008**: Advanced deduplication logic for different editions, translations, or formats (treat each ISBN as unique book).

- **OS-009**: Caching of ISBN search results across users (privacy concern, each user's search is independent)
