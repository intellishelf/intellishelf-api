# Requirements Document

## Introduction

The ISBN Book Search and Quick Add feature enables users to rapidly add books to their digital collection by entering an ISBN-10 or ISBN-13 code. The system automatically retrieves comprehensive book metadata from external sources (Google Books API as primary, Amazon Product Advertising API as fallback), validates the ISBN format, prevents duplicate entries per user, downloads and stores cover images in Azure Blob Storage, and returns complete book information. This eliminates manual data entry and ensures accurate, consistent book records across the user's collection.

## Glossary

- **ISBN System**: The International Standard Book Number system that uniquely identifies books using either 10-digit (ISBN-10) or 13-digit (ISBN-13) formats
- **User**: An authenticated individual who owns a personal digital book collection within the system
- **Book Collection**: The set of books associated with a specific authenticated user
- **External API**: Third-party book metadata services including Google Books API and Amazon Product Advertising API
- **Book Metadata**: Structured information about a book including title, author(s), publisher, publication date, description, and cover image
- **Azure Blob Storage**: Microsoft Azure cloud storage service used for storing downloaded book cover images
- **Duplicate Book**: A book with the same ISBN-10 or ISBN-13 already present in a specific user's collection
- **Normalized ISBN**: An ISBN with all hyphens, spaces, and non-numeric characters removed (except 'X' in ISBN-10 check digit)
- **Primary Source**: Google Books API, the first external service queried for book metadata
- **Fallback Source**: Amazon Product Advertising API, queried when the primary source fails or times out

## Requirements

### Requirement 1

**User Story:** As a user with a physical book, I want to add it to my digital collection by entering its ISBN, so that I don't have to manually type all the book details.

#### Acceptance Criteria

1. WHEN THE User submits a valid ISBN-13 format, THE ISBN System SHALL normalize the input by removing hyphens and spaces.

2. WHEN THE User submits a valid ISBN-10 format, THE ISBN System SHALL normalize the input by removing hyphens and spaces.

3. WHEN THE User submits an ISBN with invalid format, THE ISBN System SHALL return an error response with HTTP status 400 and message "Invalid ISBN format. Please provide a valid 10 or 13-digit ISBN."

4. WHEN THE ISBN System receives a normalized ISBN, THE ISBN System SHALL validate that ISBN-10 contains exactly 10 digits and ISBN-13 contains exactly 13 digits with prefix 978 or 979.

5. WHEN THE ISBN System validates an ISBN successfully, THE ISBN System SHALL query the Primary Source for book metadata within 5 seconds.

### Requirement 2

**User Story:** As a user, I want the system to automatically retrieve book information from reliable sources, so that my book records are accurate and complete.

#### Acceptance Criteria

1. WHEN THE Primary Source returns book metadata, THE ISBN System SHALL extract title, author(s), publisher, publication date, description, and cover image URL.

2. IF THE Primary Source fails or times out within 5 seconds, THEN THE ISBN System SHALL query the Fallback Source for book metadata within 5 seconds.

3. WHEN THE External API returns a cover image URL, THE ISBN System SHALL download the image and store it in Azure Blob Storage.

4. IF THE cover image download fails, THEN THE ISBN System SHALL store the book record with a null cover field and log the failure.

5. WHEN THE ISBN System successfully retrieves book metadata, THE ISBN System SHALL log which External API source provided the data.

### Requirement 3

**User Story:** As a user, I want to be prevented from adding the same book twice, so that my collection doesn't have duplicate entries.

#### Acceptance Criteria

1. WHEN THE ISBN System receives a valid ISBN, THE ISBN System SHALL query the Book Collection for existing books matching either ISBN-10 or ISBN-13.

2. IF THE Book Collection contains a book with matching ISBN-10 or ISBN-13 for the authenticated User, THEN THE ISBN System SHALL return an error response with HTTP status 409 and message "Book already exists in your collection."

3. WHEN THE Book Collection contains no matching ISBN for the authenticated User, THE ISBN System SHALL proceed with adding the book.

4. WHILE THE User submits the same ISBN multiple times within 10 seconds, THE ISBN System SHALL prevent creation of duplicate entries through idempotency.

5. WHEN different users submit the same ISBN, THE ISBN System SHALL allow each User to have the book in their respective Book Collection.

### Requirement 4

**User Story:** As a user, I want clear error messages when a book cannot be found, so that I understand what went wrong and what to do next.

#### Acceptance Criteria

1. IF THE Primary Source and Fallback Source both return no results for a valid ISBN, THEN THE ISBN System SHALL return an error response with HTTP status 404 and message "Book not found in our catalog. Please check the ISBN or add the book manually."

2. IF THE Primary Source and Fallback Source both fail or timeout, THEN THE ISBN System SHALL return an error response with HTTP status 503 and message "Book search temporarily unavailable. Please try again later."

3. WHEN THE ISBN System returns an error response, THE ISBN System SHALL format the response as ProblemDetails with appropriate HTTP status code.

4. WHEN THE ISBN System encounters any error condition, THE ISBN System SHALL log the error details including ISBN, user ID, and error type.

### Requirement 5

**User Story:** As a user, I want my book added to my personal collection immediately after successful lookup, so that I can start organizing it right away.

#### Acceptance Criteria

1. WHEN THE ISBN System successfully retrieves book metadata from External API, THE ISBN System SHALL create a book record associated with the authenticated User ID.

2. WHEN THE ISBN System creates a book record, THE ISBN System SHALL store both ISBN-10 and ISBN-13 formats in separate fields without dashes.

3. WHEN THE ISBN System creates a book record, THE ISBN System SHALL store the source indicator showing which External API provided the data.

4. WHEN THE ISBN System successfully stores the book record, THE ISBN System SHALL return the complete book data with HTTP status 201.

5. WHEN THE ISBN System returns the book data, THE ISBN System SHALL include all retrieved metadata fields in the same format as the existing GET book endpoint.

### Requirement 6

**User Story:** As a system administrator, I want the system to handle high concurrent load gracefully, so that multiple users can add books simultaneously without performance degradation.

#### Acceptance Criteria

1. WHEN THE ISBN System receives 100 concurrent ISBN search requests, THE ISBN System SHALL process all requests without response time exceeding 10 seconds per request.

2. WHEN THE External API rate limit is reached, THE ISBN System SHALL queue additional requests and retry with exponential backoff up to 3 attempts.

3. WHEN THE ISBN System queries External API, THE ISBN System SHALL enforce a maximum timeout of 5 seconds per source.

4. WHEN THE ISBN System processes multiple requests for the same ISBN from the same User within 10 seconds, THE ISBN System SHALL return the existing book record without creating duplicates.

### Requirement 7

**User Story:** As a user, I want the system to handle different ISBN formats flexibly, so that I can enter ISBNs as they appear on books without worrying about formatting.

#### Acceptance Criteria

1. WHEN THE User submits an ISBN containing hyphens, THE ISBN System SHALL remove all hyphens before validation.

2. WHEN THE User submits an ISBN containing spaces, THE ISBN System SHALL remove all spaces before validation.

3. WHEN THE User submits an ISBN-10 with check digit 'X', THE ISBN System SHALL preserve the 'X' character during normalization.

4. WHEN THE ISBN System normalizes an ISBN-10, THE ISBN System SHALL convert it to ISBN-13 format by adding prefix 978 and recalculating the check digit.

5. WHEN THE ISBN System stores a book record, THE ISBN System SHALL store both the original ISBN-10 (if provided) and the ISBN-13 format in separate database fields.

### Requirement 8

**User Story:** As a developer, I want comprehensive logging of ISBN lookup operations, so that I can troubleshoot issues and analyze usage patterns.

#### Acceptance Criteria

1. WHEN THE ISBN System queries the Primary Source, THE ISBN System SHALL log the ISBN, user ID, timestamp, and response status.

2. WHEN THE ISBN System falls back to the Fallback Source, THE ISBN System SHALL log the fallback event with reason for primary failure.

3. WHEN THE ISBN System successfully adds a book, THE ISBN System SHALL log the book ID, ISBN, user ID, source, and timestamp.

4. WHEN THE ISBN System encounters any error, THE ISBN System SHALL log the error type, ISBN, user ID, and error message.

5. WHEN THE ISBN System downloads a cover image, THE ISBN System SHALL log the source URL, Azure Blob Storage path, and download status.
