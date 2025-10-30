# ISBN Book Search Data Model

**Feature**: 001-isbn-book-search  
**Date**: 2025-01-29  
**Status**: Phase 1 Complete

---

## Table of Contents
1. [Domain Entities](#domain-entities)
2. [Value Objects](#value-objects)
3. [Service Interfaces](#service-interfaces)
4. [Data Transfer Objects](#data-transfer-objects)
5. [Database Schema](#database-schema)
6. [Error Types](#error-types)

---

## Domain Entities

### BookEntity (Existing, Extended)
**File**: `src/Intellishelf.Data/Books/Entities/BookEntity.cs`

```csharp
using MongoDB.Bson;

namespace Intellishelf.Data.Books.Entities;

public class BookEntity : EntityBase
{
    public const string CollectionName = "Books";

    public required ObjectId UserId { get; init; }

    public required string Title { get; init; }
    
    public string[]? Authors { get; init; }
    
    public DateTime? PublicationDate { get; init; }
    
    /// <summary>
    /// ISBN-10 identifier (10 digits, optional hyphens, may end with 'X').
    /// Nullable: Books published after 2007 may only have ISBN-13.
    /// Stored without hyphens for consistency.
    /// </summary>
    public string? Isbn10 { get; init; }

    /// <summary>
    /// ISBN-13 identifier (13 digits, prefix 978/979, optional hyphens).
    /// Nullable: May not be available for all books.
    /// Stored without hyphens for consistency.
    /// </summary>
    public string? Isbn13 { get; init; }
    
    public string? Publisher { get; init; }
    
    public int? Pages { get; init; }
    
    /// <summary>
    /// Azure Blob Storage URL for cover image.
    /// Format: https://{account}.blob.core.windows.net/book-covers/{userId}/books/{bookId}.jpg
    /// </summary>
    public string? CoverImageUrl { get; init; }
    
    public string? Annotation { get; init; }
    
    public string? Description { get; init; }
    
    public string[]? Tags { get; init; }

    /// <summary>
    /// External API source for book metadata.
    /// Used for analytics and debugging.
    /// </summary>
    public BookSource? Source { get; init; }
    
    public required DateTime CreatedDate { get; init; }
    
    public required DateTime ModifiedDate { get; init; }
}

/// <summary>
/// Book metadata source enum
/// </summary>
public enum BookSource
{
    Google,
    Amazon,
    Manual
}
```

**Changes from Existing**:
- **Added**: `Source` (enum: Google/Amazon/Manual)
- **Verify**: `Isbn10` and `Isbn13` already exist (both nullable)
- **Verify**: `CoverImageUrl` exists and is nullable
- **Verify**: `Authors` is `string[]` (correct in existing entity)
- **Note**: Uses existing field names (`CoverImageUrl` instead of `CoverBlobUrl`, `PublicationDate` instead of `PublishedDate`)

---

## Value Objects

**None**: This codebase follows anemic domain model pattern. Entities are pure data containers; all validation and business logic resides in service classes.

See `BookService` for ISBN validation and normalization helper methods.

### BookMetadata (New)
**File**: `src/Intellishelf.Domain/Books/BookMetadata.cs`

```csharp
namespace Intellishelf.Domain.Books;

/// <summary>
/// Metadata retrieved from external APIs (Google Books or Amazon).
/// Intermediate DTO before converting to BookEntity.
/// </summary>
public sealed record BookMetadata(
    string Title,
    string[] Authors,
    string? Publisher,
    DateTime? PublicationDate,
    string? Description,
    int? PageCount,
    string? CoverImageUrl,
    BookSource Source
);
```

---

## Service Interfaces

### IBookService (Existing, Extended)
**File**: `src/Intellishelf.Domain/Books/IBookService.cs`

```csharp
namespace Intellishelf.Domain.Books;

public interface IBookService
{
    // Existing methods...
    // Task<Result<BookEntity>> GetBookByIdAsync(string userId, string bookId);
    // Task<Result<List<BookEntity>>> GetAllBooksAsync(string userId);
    // etc.

    /// <summary>
    /// Adds a book to the user's collection by ISBN.
    /// Validates ISBN, checks for duplicates, fetches metadata from external APIs,
    /// downloads cover image, and persists to database.
    /// </summary>
    /// <param name="userId">Authenticated user ID (ObjectId as string)</param>
    /// <param name="isbn">User-provided ISBN (10 or 13 digits, with or without hyphens)</param>
    /// <returns>
    /// Success: Created BookEntity
    /// Failure: InvalidIsbn, DuplicateBook, BookNotFound, ExternalApiFailure errors
    /// </returns>
    Task<Result<BookEntity>> AddBookByIsbnAsync(string userId, string isbn);
}
```

### BookService (Existing, Extended)
**File**: `src/Intellishelf.Domain/Books/BookService.cs`

```csharp
namespace Intellishelf.Domain.Books;

public class BookService(
    IBookDao bookDao,
    IIsbnLookupService isbnLookupService,
    IImageStorageService imageStorageService,
    ILogger<BookService> logger) : IBookService
{
    public async Task<Result<BookEntity>> AddBookByIsbnAsync(string userId, string isbn)
    {
        // 1. Validate and normalize ISBN
        var normalizedIsbn = NormalizeIsbn(isbn);
        if (!IsValidIsbnFormat(normalizedIsbn, out var isbn10, out var isbn13))
        {
            return new Error(BookErrorCodes.InvalidIsbn, 
                $"Invalid ISBN format: {isbn}. Must be 10 or 13 digits.");
        }

        var userObjectId = ObjectId.Parse(userId);

        // 2. Check for duplicates
        var existingBook = await bookDao.FindByIsbnAsync(userObjectId, isbn10, isbn13);
        if (existingBook != null)
        {
            return new Error(BookErrorCodes.DuplicateBook, 
                "Book already exists in your collection");
        }

        // 3. Lookup metadata from external APIs
        var metadataResult = await isbnLookupService.LookupByIsbnAsync(isbn13 ?? isbn10!);
        if (!metadataResult.IsSuccess)
        {
            return metadataResult.Error;
        }

        var metadata = metadataResult.Value;

        // 4. Download cover image (optional, don't fail if missing)
        string? coverUrl = null;
        if (!string.IsNullOrEmpty(metadata.CoverImageUrl))
        {
            var imageResult = await imageStorageService.DownloadAndStoreAsync(
                metadata.CoverImageUrl,
                $"{userId}/books/{Guid.NewGuid()}.jpg");

            if (imageResult.IsSuccess)
            {
                coverUrl = imageResult.Value;
            }
            else
            {
                logger.LogWarning("Failed to download cover image: {Error}", 
                    imageResult.Error.Message);
            }
        }

        // 5. Create and persist book entity
        var book = new BookEntity
        {
            UserId = userObjectId,
            Title = metadata.Title,
            Authors = metadata.Authors,
            Publisher = metadata.Publisher,
            PublicationDate = metadata.PublicationDate,
            Isbn10 = isbn10,
            Isbn13 = isbn13,
            Description = metadata.Description,
            Pages = metadata.PageCount,
            CoverImageUrl = coverUrl,
            Source = metadata.Source,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        return await bookDao.InsertAsync(book);
    }

    // Private helper methods for ISBN validation
    private static string NormalizeIsbn(string isbn)
        => isbn.Replace("-", "").Replace(" ", "").ToUpperInvariant().Trim();

    private static bool IsValidIsbnFormat(string normalized, out string? isbn10, out string? isbn13)
    {
        isbn10 = null;
        isbn13 = null;

        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        // Check ISBN-10 format
        if (normalized.Length == 10 && 
            normalized[..9].All(char.IsDigit) && 
            (char.IsDigit(normalized[9]) || normalized[9] == 'X'))
        {
            isbn10 = normalized;
            // Simple conversion to ISBN-13 (prepend 978, placeholder check digit)
            isbn13 = "978" + normalized[..9] + "0";
            return true;
        }

        // Check ISBN-13 format
        if (normalized.Length == 13 && 
            normalized.All(char.IsDigit) && 
            (normalized.StartsWith("978") || normalized.StartsWith("979")))
        {
            isbn13 = normalized;
            // Optionally derive ISBN-10 if prefix is 978
            if (normalized.StartsWith("978"))
            {
                isbn10 = normalized.Substring(3, 9) + "0";
            }
            return true;
        }

        return false;
    }
}
```

### IIsbnLookupService (New)
**File**: `src/Intellishelf.Domain/Books/IIsbnLookupService.cs`

```csharp
namespace Intellishelf.Domain.Books;

/// <summary>
/// Service for fetching book metadata from external APIs (Google Books, Amazon).
/// Implements fallback strategy: Google Books (primary) → Amazon (fallback).
/// </summary>
public interface IIsbnLookupService
{
    /// <summary>
    /// Looks up book metadata by ISBN from external APIs.
    /// Tries Google Books first, falls back to Amazon if not found.
    /// </summary>
    /// <param name="isbn">Validated Isbn value object</param>
    /// <returns>
    /// Success: BookMetadata with source tag
    /// Failure: BookNotFound, ExternalApiFailure errors
    /// </returns>
    Task<Result<BookMetadata>> LookupByIsbnAsync(Isbn isbn);
}
```

### IBookDao (Existing, Extended)
**File**: `src/Intellishelf.Data/Books/IBookDao.cs`

```csharp
using MongoDB.Bson;

namespace Intellishelf.Data.Books;

public interface IBookDao
{
    // Existing methods...
    // Task<Result<BookEntity>> GetByIdAsync(ObjectId userId, ObjectId bookId);
    // Task<Result<List<BookEntity>>> GetAllAsync(ObjectId userId);
    // etc.

    /// <summary>
    /// Finds a book by ISBN-10 or ISBN-13 for a specific user.
    /// Used for duplicate detection before adding new book.
    /// </summary>
    /// <param name="userId">User ObjectId</param>
    /// <param name="isbn10">Normalized ISBN-10 (nullable)</param>
    /// <param name="isbn13">Normalized ISBN-13 (nullable)</param>
    /// <returns>Existing BookEntity if found, null otherwise</returns>
    Task<BookEntity?> FindByIsbnAsync(ObjectId userId, string? isbn10, string? isbn13);

    /// <summary>
    /// Inserts a new book into the user's collection.
    /// </summary>
    Task<Result<BookEntity>> InsertAsync(BookEntity book);
}
```

---

## Data Transfer Objects

### AddBookByIsbnRequest
**File**: `src/Intellishelf.Api/Contracts/Books/AddBookByIsbnRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Intellishelf.Api.Contracts.Books;

/// <summary>
/// Request to add a book by ISBN.
/// </summary>
public sealed record AddBookByIsbnRequest
{
    /// <summary>
    /// ISBN-10 or ISBN-13 (with or without hyphens).
    /// Examples: "0-306-40615-2", "9780306406157", "0306406152"
    /// </summary>
    [Required(ErrorMessage = "ISBN is required")]
    [StringLength(17, MinimumLength = 10, ErrorMessage = "ISBN must be between 10-17 characters")]
    public required string Isbn { get; init; }
}
```

### AddBookByIsbnResponse
**File**: `src/Intellishelf.Api/Contracts/Books/AddBookByIsbnResponse.cs`

```csharp
namespace Intellishelf.Api.Contracts.Books;

/// <summary>
/// Response after successfully adding a book by ISBN.
/// </summary>
public sealed record AddBookByIsbnResponse
{
    public required string BookId { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    public required string Title { get; init; }
    public required string[] Authors { get; init; }
    public string? Publisher { get; init; }
    public DateTime? PublicationDate { get; init; }
    public string? Description { get; init; }
    public string? CoverUrl { get; init; }
    public int? Pages { get; init; }
    public required string Source { get; init; }
    public required DateTime CreatedAt { get; init; }
}
```

### BookMapper (New)
**File**: `src/Intellishelf.Api/Mappers/Books/BookMapper.cs`

```csharp
using Intellishelf.Api.Contracts.Books;
using Intellishelf.Data.Books.Entities;

namespace Intellishelf.Api.Mappers.Books;

public static class BookMapper
{
    public static AddBookByIsbnResponse ToAddBookByIsbnResponse(this BookEntity book)
    {
        return new AddBookByIsbnResponse
        {
            BookId = book.Id.ToString(),
            Isbn10 = book.Isbn10,
            Isbn13 = book.Isbn13,
            Title = book.Title,
            Authors = book.Authors ?? Array.Empty<string>(),
            Publisher = book.Publisher,
            PublicationDate = book.PublicationDate,
            Description = book.Description,
            CoverUrl = book.CoverImageUrl,
            Pages = book.Pages,
            Source = book.Source?.ToString() ?? "Manual",
            CreatedAt = book.CreatedDate
        };
    }
}
```

---

## Database Schema

### MongoDB Collection: `books`
**Existing collection**, schema extended with new fields.

#### Indexes
```javascript
// Unique compound index: userId + isbn13 (primary duplicate detection)
db.books.createIndex(
  { "userId": 1, "isbn13": 1 },
  { unique: true, name: "idx_userId_isbn13" }
)

// Non-unique compound index: userId + isbn10 (for ISBN-10 search)
db.books.createIndex(
  { "userId": 1, "isbn10": 1 },
  { name: "idx_userId_isbn10" }
)

// Index for createdAt (for sorting/filtering by date)
db.books.createIndex(
  { "createdAt": -1 },
  { name: "idx_createdAt" }
)
```

#### Sample Document
```json
{
  "_id": ObjectId("507f1f77bcf86cd799439011"),
  "userId": ObjectId("507f191e810c19729de860ea"),
  "title": "The Pragmatic Programmer",
  "authors": ["Andrew Hunt", "David Thomas"],
  "publicationDate": ISODate("1999-10-30T00:00:00Z"),
  "isbn10": "0306406152",
  "isbn13": "9780306406157",
  "publisher": "Addison-Wesley",
  "pages": 352,
  "coverImageUrl": "https://intellishelf.blob.core.windows.net/book-covers/507f191e810c19729de860ea/books/507f1f77bcf86cd799439011.jpg",
  "description": "Your journey to mastery...",
  "annotation": null,
  "tags": ["programming", "software-engineering"],
  "source": "Google",
  "createdDate": ISODate("2025-01-29T10:30:00Z"),
  "modifiedDate": ISODate("2025-01-29T10:30:00Z")
}
```

#### Migration Notes
- **Existing books** without `isbn10`/`isbn13`: Set to `null` (no migration script needed, nullable fields)
- **Index creation**: Run in `BookDao` constructor or startup configuration
- **Backward compatibility**: Existing endpoints unaffected (new fields optional)

---

## Error Types

### Error Codes (Intellishelf.Common)
**File**: `src/Intellishelf.Common/TryResult/BookErrorCodes.cs` (or add to existing error codes)

Add the following error code constants:

```csharp
public static class BookErrorCodes
{
    // Existing codes...
    public const string BookNotFound = "BOOK_NOT_FOUND";
    
    // New ISBN-specific codes
    public const string InvalidIsbn = "INVALID_ISBN";
    public const string DuplicateBook = "DUPLICATE_BOOK";
    public const string ExternalApiFailure = "EXTERNAL_API_FAILURE";
}
```

### Error Usage in Services
**Pattern**: Create `Error` instances with error code and message

```csharp
// Invalid ISBN format
return new Error(BookErrorCodes.InvalidIsbn, "Invalid ISBN format: input must be 10 or 13 digits");

// Book not found in external APIs
return new Error(BookErrorCodes.BookNotFound, $"Book not found in Google Books or Amazon");

// Duplicate book in collection
return new Error(BookErrorCodes.DuplicateBook, "Book already exists in your collection");

// External API failure
return new Error(BookErrorCodes.ExternalApiFailure, $"External API failure ({source}): {details}");
```

### Error Mapping in Controller
**File**: `src/Intellishelf.Api/Controllers/BooksController.cs`

```csharp
private IActionResult HandleBookError(Error error)
{
    var statusCode = error.Code switch
    {
        BookErrorCodes.InvalidIsbn => 400,
        BookErrorCodes.BookNotFound => 404,
        BookErrorCodes.DuplicateBook => 409,
        BookErrorCodes.ExternalApiFailure => 503,
        _ => 500
    };

    return StatusCode(statusCode, new ProblemDetails 
    { 
        Status = statusCode,
        Title = GetErrorTitle(statusCode),
        Detail = error.Message
    });
}

private static string GetErrorTitle(int statusCode) => statusCode switch
{
    400 => "Invalid Request",
    404 => "Not Found",
    409 => "Conflict",
    503 => "Service Unavailable",
    _ => "Internal Server Error"
};
```

---

## Data Flow Diagram

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ POST /api/books/isbn { "isbn": "0-306-40615-2" }
       ▼
┌─────────────────────────────────────────────────────────────┐
│ BooksController.AddBookByIsbn()                             │
│ - Validate request (ModelState)                             │
│ - Extract userId from claims                                │
│ - Call BookService.AddBookByIsbnAsync(userId, isbn)         │
└───────────────────────────┬─────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ BookService.AddBookByIsbnAsync()                            │
│ 1. Validate ISBN format → Isbn.Create()                     │
│ 2. Check for duplicates → BookDao.FindByIsbnAsync()         │
│ 3. Lookup metadata → IsbnLookupService.LookupByIsbnAsync()  │
│ 4. Download cover → ImageStorageService.DownloadAndStore()  │
│ 5. Create BookEntity and persist → BookDao.InsertAsync()    │
└────────┬────────────────────────────────────┬───────────────┘
         │                                    │
         │ (Step 3)                           │ (Step 5)
         ▼                                    ▼
┌────────────────────────┐         ┌──────────────────────┐
│ IsbnLookupService      │         │ BookDao              │
│ - GoogleBooksClient    │         │ - MongoDB Insert     │
│ - AmazonProductClient  │         │ - Index enforcement  │
│ - Fallback logic       │         └──────────────────────┘
└────────────────────────┘
         │
         ├─ Google Books API ───► 200 OK → Return metadata
         └─ Amazon API (fallback) ───► 200 OK → Return metadata
```

---

**Next Steps**: Generate contracts OpenAPI spec and quickstart.md
