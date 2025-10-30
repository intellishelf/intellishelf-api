# ISBN Book Search Research

**Feature**: 001-isbn-book-search  
**Date**: 2025-01-29  
**Status**: Phase 0 Complete

---

## Table of Contents
1. [ISBN Validation Algorithms](#isbn-validation-algorithms)
2. [Google Books API Integration](#google-books-api-integration)
3. [Amazon Product Advertising API Integration](#amazon-product-advertising-api-integration)
4. [External API Fallback Strategy](#external-api-fallback-strategy)
5. [Azure Blob Storage for Cover Images](#azure-blob-storage-for-cover-images)
6. [MongoDB Dual ISBN Indexing](#mongodb-dual-isbn-indexing)
7. [Performance Considerations](#performance-considerations)
8. [Dependencies and NuGet Packages](#dependencies-and-nuget-packages)

---

## ISBN Validation Algorithms

### ISBN-10 Format
- **Structure**: 10 digits (0-9) with optional hyphens, last character can be 'X' (representing 10)
- **Regex**: `^(?:\d{9}[\dX]|\d{1,5}-\d{1,7}-\d{1,7}-[\dX])$`
- **Validation**: Basic format check only (length and character validation)
- **Example**: `0-306-40615-2` or `0306406152`

### ISBN-13 Format
- **Structure**: 13 digits (0-9) with optional hyphens, prefix 978 or 979
- **Regex**: `^(?:97[89]\d{10}|97[89]-\d{1,5}-\d{1,7}-\d{1,7}-\d)$`
- **Validation**: Basic format check only (length, prefix, and character validation)
- **Example**: `978-0-306-40615-7` or `9780306406157`

### ISBN-10 to ISBN-13 Conversion
- **Simplified Algorithm**:
  1. Remove hyphens from ISBN-10
  2. Remove last character (check digit)
  3. Prepend "978" to the remaining 9 digits
  4. Append "0" as placeholder check digit (or calculate if needed by external API)
- **Example**: `0-306-40615-2` → `9780306406150`
- **Note**: Exact check digit calculation not required; external APIs will normalize

### Implementation Notes
- **Simplified Validation**: No check digit verification (modulus calculations removed)
- Format validation only: correct length, allowed characters, proper prefix for ISBN-13
- Store both formats in MongoDB to optimize search
- External APIs (Google Books) will handle ISBN normalization and validation

---

## Google Books API Integration

### API Overview
- **Endpoint**: `https://www.googleapis.com/books/v1/volumes`
- **Authentication**: API Key (required for production, rate limits apply)
- **Rate Limits**: 1,000 requests/day (free tier), can request quota increase
- **Cost**: Free tier sufficient for MVP

### Search by ISBN
```http
GET https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn}&key={API_KEY}
```

**Response Structure** (Success):
```json
{
  "kind": "books#volumes",
  "totalItems": 1,
  "items": [
    {
      "id": "unique-volume-id",
      "volumeInfo": {
        "title": "Book Title",
        "subtitle": "Optional Subtitle",
        "authors": ["Author 1", "Author 2"],
        "publisher": "Publisher Name",
        "publishedDate": "2023-01-15",
        "description": "Full description text...",
        "industryIdentifiers": [
          { "type": "ISBN_10", "identifier": "0306406152" },
          { "type": "ISBN_13", "identifier": "9780306406157" }
        ],
        "pageCount": 352,
        "categories": ["Fiction", "Mystery"],
        "imageLinks": {
          "smallThumbnail": "http://books.google.com/books/content?id=...&zoom=5",
          "thumbnail": "http://books.google.com/books/content?id=...&zoom=1",
          "small": "http://books.google.com/books/content?id=...&zoom=2",
          "medium": "http://books.google.com/books/content?id=...&zoom=3",
          "large": "http://books.google.com/books/content?id=...&zoom=4"
        },
        "language": "en"
      }
    }
  ]
}
```

**Response Structure** (Not Found):
```json
{
  "kind": "books#volumes",
  "totalItems": 0
}
```

### NuGet Package
- **Package**: `Google.Apis.Books.v1` (official Google API client)
- **Latest Version**: 1.68.0.3463 (as of 2024)
- **Install**: `dotnet add package Google.Apis.Books.v1`

### Integration Pattern
```csharp
using Google.Apis.Books.v1;
using Google.Apis.Services;

public class GoogleBooksClient
{
    private readonly BooksService _service;

    public GoogleBooksClient(string apiKey)
    {
        _service = new BooksService(new BaseClientService.Initializer
        {
            ApiKey = apiKey,
            ApplicationName = "Intellishelf"
        });
    }

    public async Task<TryResult<BookMetadata>> GetBookByIsbnAsync(string isbn)
    {
        var request = _service.Volumes.List($"isbn:{isbn}");
        var response = await request.ExecuteAsync();
        
        if (response.TotalItems == 0)
            return TryResult<BookMetadata>.Failure(Error.BookNotFound("Google Books"));
        
        var volume = response.Items[0];
        // Map volumeInfo to BookMetadata
        return TryResult<BookMetadata>.Success(metadata);
    }
}
```

### Error Handling
- **HTTP 403**: API key invalid or quota exceeded → return `ExternalApiFailure` error
- **HTTP 429**: Rate limit exceeded → retry with exponential backoff or fallback to Amazon
- **HTTP 500**: Google Books service error → fallback to Amazon
- **totalItems == 0**: Book not found → fallback to Amazon

### Configuration
```json
// appsettings.json
{
  "ExternalApis": {
    "GoogleBooks": {
      "ApiKey": "[from user secrets]",
      "BaseUrl": "https://www.googleapis.com/books/v1",
      "TimeoutSeconds": 5
    }
  }
}
```

---

## Amazon Product Advertising API Integration

### API Overview
- **Endpoint**: `https://webservices.amazon.com/paapi5/searchitems`
- **Authentication**: Access Key + Secret Key + Signature v4
- **Rate Limits**: 1 request/second, 8,640 requests/day (free tier)
- **Cost**: Free if affiliate sales generated, otherwise paid tier required
- **⚠️ Complexity**: Requires AWS Signature v4 signing, more complex than Google Books

### Search by ISBN
```http
POST https://webservices.amazon.com/paapi5/searchitems
Content-Type: application/json
Authorization: AWS4-HMAC-SHA256 Credential=...

{
  "Keywords": "9780306406157",
  "SearchIndex": "Books",
  "ItemCount": 1,
  "Resources": [
    "ItemInfo.Title",
    "ItemInfo.ByLineInfo",
    "ItemInfo.ContentInfo",
    "Images.Primary.Large"
  ],
  "PartnerTag": "your-associate-tag",
  "PartnerType": "Associates",
  "Marketplace": "www.amazon.com"
}
```

**Response Structure** (Success):
```json
{
  "SearchResult": {
    "Items": [
      {
        "ASIN": "B00EXAMPLE",
        "ItemInfo": {
          "Title": { "DisplayValue": "Book Title" },
          "ByLineInfo": {
            "Contributors": [
              { "Name": "Author 1", "Role": "Author" }
            ]
          },
          "ContentInfo": {
            "PublicationDate": { "DisplayValue": "2023-01-15" },
            "Publisher": { "DisplayValue": "Publisher Name" }
          }
        },
        "Images": {
          "Primary": {
            "Large": { "URL": "https://m.media-amazon.com/images/..." }
          }
        }
      }
    ]
  }
}
```

### Custom HTTP Client Implementation
- **Approach**: Build custom HTTP client with AWS Signature v4
- **No third-party packages**: Implement signing logic manually
- **Reference**: [AWS Signature v4 Documentation](https://docs.aws.amazon.com/general/latest/gr/signature-version-4.html)

### Integration Pattern (Custom Implementation)
```csharp
using System.Security.Cryptography;
using System.Text;

public class AmazonProductClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _partnerTag;
    private readonly string _marketplace;

    public AmazonProductClient(HttpClient httpClient, string accessKey, string secretKey, string partnerTag, string marketplace)
    {
        _httpClient = httpClient;
        _accessKey = accessKey;
        _secretKey = secretKey;
        _partnerTag = partnerTag;
        _marketplace = marketplace;
    }

    public async Task<BookMetadata?> GetBookByIsbnAsync(string isbn)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var requestBody = CreateSearchItemsRequest(isbn);
        var signature = GenerateAwsSignatureV4(requestBody, timestamp);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://webservices.amazon.com/paapi5/searchitems")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", signature);
        request.Headers.Add("X-Amz-Date", timestamp);

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Amazon API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AmazonSearchResponse>(json);
            return MapToBookMetadata(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon API call failed for ISBN {Isbn}", isbn);
            return null;
        }
    }

    private string GenerateAwsSignatureV4(string requestBody, string timestamp)
    {
        // AWS Signature v4 implementation
        // 1. Create canonical request
        // 2. Create string to sign
        // 3. Calculate signature using HMAC-SHA256
        // 4. Build authorization header
        // Full implementation details: https://docs.aws.amazon.com/general/latest/gr/sigv4-create-canonical-request.html
        throw new NotImplementedException("AWS Signature v4 signing to be implemented");
    }
}
```

**Note**: Custom implementation required due to no official .NET SDK. AWS Signature v4 signing is complex but well-documented.

### Error Handling
- **HTTP 403**: Invalid credentials → log error, skip Amazon fallback
- **HTTP 429**: Rate limit exceeded → return `ExternalApiFailure`
- **HTTP 503**: Amazon service unavailable → return `ExternalApiFailure`
- **No items found**: Return `BookNotFound` error

### Configuration
```json
// appsettings.json (use User Secrets for keys)
{
  "ExternalApis": {
    "Amazon": {
      "AccessKey": "[from user secrets]",
      "SecretKey": "[from user secrets]",
      "PartnerTag": "your-associate-tag",
      "Marketplace": "www.amazon.com",
      "TimeoutSeconds": 5
    }
  }
}
```

---

## External API Fallback Strategy

### Decision: Google Books Primary, Amazon Fallback
**Rationale** (from spec clarification):
- Google Books API is simpler (API key only, no signature required)
- Free tier is sufficient for MVP (1,000 requests/day)
- Amazon API requires affiliate program participation and complex auth
- Cost-effective: Google Books → Amazon → Error (instead of parallel calls)

### Implementation Flow
```
1. Validate ISBN format (ISBN-10 or ISBN-13)
2. Convert ISBN-10 to ISBN-13 if needed (for consistency)
3. Call Google Books API
   └─ Success? → Return book metadata
   └─ HTTP 403/429/500? → Fallback to Amazon
   └─ Not Found? → Fallback to Amazon
4. Call Amazon Product API
   └─ Success? → Return book metadata
   └─ Failure? → Return Error.BookNotFound("both sources")
5. Download cover image from returned URL
6. Store book in MongoDB with source tag ("Google" or "Amazon")
```

### IsbnLookupService Implementation Outline
```csharp
public class IsbnLookupService : IIsbnLookupService
{
    private readonly GoogleBooksClient _googleBooksClient;
    private readonly AmazonProductClient _amazonProductClient;
    private readonly ILogger<IsbnLookupService> _logger;

    public async Task<TryResult<BookMetadata>> LookupByIsbnAsync(string isbn)
    {
        // Try Google Books first
        var googleResult = await _googleBooksClient.GetBookByIsbnAsync(isbn);
        if (googleResult.IsSuccess)
        {
            _logger.LogInformation("Found book via Google Books: {Isbn}", isbn);
            return googleResult; // Include source = "Google"
        }

        _logger.LogWarning("Google Books lookup failed for {Isbn}: {Error}. Trying Amazon...", 
            isbn, googleResult.Error);

        // Fallback to Amazon
        var amazonResult = await _amazonProductClient.GetBookByIsbnAsync(isbn);
        if (amazonResult.IsSuccess)
        {
            _logger.LogInformation("Found book via Amazon: {Isbn}", isbn);
            return amazonResult; // Include source = "Amazon"
        }

        _logger.LogError("Both Google Books and Amazon failed for {Isbn}", isbn);
        return TryResult<BookMetadata>.Failure(
            Error.BookNotFound($"ISBN {isbn} not found in Google Books or Amazon"));
    }
}
```

### Retry Policy (Optional for Phase 2)
- Use Polly library for transient HTTP error retries (429, 503)
- Exponential backoff: 1s, 2s, 4s (max 3 retries)
- Circuit breaker: After 5 consecutive failures, skip API for 30 seconds

---

## Azure Blob Storage for Cover Images

### Decision: Download and Persist in Blob Storage
**Rationale** (from spec clarification):
- External API URLs may expire or change (Google/Amazon CDN links)
- Blob Storage provides stable, permanent URLs for frontend display
- Enables offline access and faster loading (no external dependencies)

### Integration with Existing IImageStorageService
Current interface in `Intellishelf.Domain/Files/IImageStorageService.cs`:
```csharp
public interface IImageStorageService
{
    Task<TryResult<string>> UploadAsync(Stream imageStream, string fileName, string contentType);
    Task<TryResult<string>> DownloadAndStoreAsync(string imageUrl, string fileName);
    Task<TryResult<bool>> DeleteAsync(string fileName);
}
```

**Use `DownloadAndStoreAsync`** for ISBN book cover images:
1. Download image from Google Books/Amazon URL
2. Generate unique filename: `{userId}/books/{bookId}.jpg`
3. Upload to Azure Blob Storage container `book-covers`
4. Return blob URL (e.g., `https://{account}.blob.core.windows.net/book-covers/{userId}/books/{bookId}.jpg`)
5. Store blob URL in `BookEntity.CoverBlobUrl`

### Image Processing
- **Validation**: Use existing `ImageFileValidator` to verify image format (JPEG/PNG)
- **Resizing**: Optional (Phase 2) - resize to standard thumbnail size (e.g., 300x450px)
- **Fallback**: If image download fails, store `null` in `CoverBlobUrl` (book still saved without cover)

### Example Usage in BookService
```csharp
var metadata = await _isbnLookupService.LookupByIsbnAsync(isbn);
if (!metadata.IsSuccess)
    return metadata.Error;

string? coverBlobUrl = null;
if (!string.IsNullOrEmpty(metadata.Value.CoverImageUrl))
{
    var imageResult = await _imageStorageService.DownloadAndStoreAsync(
        metadata.Value.CoverImageUrl, 
        $"{userId}/books/{Guid.NewGuid()}.jpg");
    
    if (imageResult.IsSuccess)
        coverBlobUrl = imageResult.Value;
    else
        _logger.LogWarning("Failed to download cover image: {Error}", imageResult.Error);
}

var book = new BookEntity
{
    // ... other fields
    CoverBlobUrl = coverBlobUrl,
    Source = metadata.Value.Source // "Google" or "Amazon"
};
```

---

## MongoDB Dual ISBN Indexing

### Decision: Store Both ISBN-10 and ISBN-13
**Rationale** (from spec clarification):
- Enables fast lookup without runtime conversion
- Supports user input in either format (ISBN-10 or ISBN-13)
- Optimizes duplicate detection (single query instead of two conversions)

### BookEntity Schema Extension
```csharp
public class BookEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("userId")]
    public string UserId { get; set; } = null!;

    [BsonElement("isbn10")]
    public string? Isbn10 { get; set; } // Nullable: some books only have ISBN-13

    [BsonElement("isbn13")]
    public string Isbn13 { get; set; } = null!; // Required: always convert ISBN-10 to ISBN-13

    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("authors")]
    public List<string> Authors { get; set; } = new();

    [BsonElement("publisher")]
    public string? Publisher { get; set; }

    [BsonElement("publishedDate")]
    public string? PublishedDate { get; set; } // Store as string (YYYY-MM-DD or YYYY)

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("coverBlobUrl")]
    public string? CoverBlobUrl { get; set; }

    [BsonElement("source")]
    public string Source { get; set; } = null!; // "Google" or "Amazon"

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Index Strategy
Create compound indexes for optimal query performance:

```csharp
// BookDao.cs or startup configuration
await collection.Indexes.CreateManyAsync(new[]
{
    // Unique compound index: userId + isbn13 (primary duplicate detection)
    new CreateIndexModel<BookEntity>(
        Builders<BookEntity>.IndexKeys
            .Ascending(b => b.UserId)
            .Ascending(b => b.Isbn13),
        new CreateIndexOptions { Unique = true, Name = "idx_userId_isbn13" }
    ),
    
    // Non-unique compound index: userId + isbn10 (for ISBN-10 search)
    new CreateIndexModel<BookEntity>(
        Builders<BookEntity>.IndexKeys
            .Ascending(b => b.UserId)
            .Ascending(b => b.Isbn10),
        new CreateIndexOptions { Name = "idx_userId_isbn10" }
    ),
    
    // Index for createdAt (for sorting/filtering by date)
    new CreateIndexModel<BookEntity>(
        Builders<BookEntity>.IndexKeys
            .Descending(b => b.CreatedAt),
        new CreateIndexOptions { Name = "idx_createdAt" }
    )
});
```

### Duplicate Detection Query
```csharp
public async Task<BookEntity?> FindByIsbnAsync(string userId, string isbn10, string isbn13)
{
    var filter = Builders<BookEntity>.Filter.And(
        Builders<BookEntity>.Filter.Eq(b => b.UserId, userId),
        Builders<BookEntity>.Filter.Or(
            Builders<BookEntity>.Filter.Eq(b => b.Isbn10, isbn10),
            Builders<BookEntity>.Filter.Eq(b => b.Isbn13, isbn13)
        )
    );

    return await _collection.Find(filter).FirstOrDefaultAsync();
}
```

**Performance**:
- Single query checks both ISBN formats
- Uses compound index `idx_userId_isbn13` or `idx_userId_isbn10`
- Expected query time: <10ms for indexed lookup

---

## Performance Considerations

### Success Criteria (from spec)
- **Initial lookup**: < 5 seconds (P1 - MUST MEET)
- **Cached responses**: < 200ms (P1 - MUST MEET)
- **Duplicate detection**: < 100ms (P2 - SHOULD MEET)

### Optimization Strategies

#### 1. HTTP Client Timeouts
```csharp
// Configure HttpClient with aggressive timeouts
services.AddHttpClient<GoogleBooksClient>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(5));

services.AddHttpClient<AmazonProductClient>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(5));
```

#### 2. Parallel Duplicate Check and Image Download (Optional)
```csharp
// After successful ISBN lookup, run duplicate check and image download in parallel
var duplicateCheckTask = _bookDao.FindByIsbnAsync(userId, isbn10, isbn13);
var imageDownloadTask = _imageStorageService.DownloadAndStoreAsync(metadata.CoverImageUrl, fileName);

await Task.WhenAll(duplicateCheckTask, imageDownloadTask);

if (await duplicateCheckTask != null)
    return Error.DuplicateBook("Book already in your collection");

var coverBlobUrl = (await imageDownloadTask).Value;
```

#### 3. MongoDB Connection Pooling
Existing `DatabaseConfig` should already have connection pooling configured:
```json
{
  "Database": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "intellishelf",
    "MaxPoolSize": 100, // Ensure this is set
    "MinPoolSize": 10
  }
}
```

#### 4. Caching Strategy (Phase 2)
- **In-Memory Cache**: Store ISBN → BookMetadata for 1 hour (reduce external API calls)
- **Redis Distributed Cache**: For multi-instance deployments
- **Not implemented in Phase 1** (MVP focus on correctness over performance)

### Bottleneck Analysis
| Operation | Expected Time | Optimization |
|-----------|--------------|--------------|
| ISBN Validation | <1ms | Pure computation, no optimization needed |
| Google Books API | 500ms - 2s | HTTP timeout 5s, fallback to Amazon |
| Amazon API | 500ms - 2s | HTTP timeout 5s, final fallback |
| Image Download | 200ms - 1s | Azure Blob upload is fast, acceptable delay |
| Duplicate Check | <10ms | MongoDB indexed query, acceptable |
| MongoDB Insert | <50ms | Async write, non-blocking |

**Worst Case**: 5s (Google timeout) + 5s (Amazon timeout) + 1s (image) = **11 seconds**  
**Mitigation**: Set HTTP timeouts to 3s each → 3s + 3s + 1s = **7 seconds** (within tolerance)

---

## Dependencies and NuGet Packages

### New Dependencies
```xml
<!-- Intellishelf.Domain.csproj -->
<PackageReference Include="Google.Apis.Books.v1" Version="1.68.0.3463" />
<!-- No Amazon package - custom HTTP client implementation -->
```

### Existing Dependencies (Verify Compatibility)
- `MongoDB.Driver` (already in use)
- `Azure.Storage.Blobs` (already in use for IImageStorageService)
- `Microsoft.Extensions.Http` (for HttpClient factory - needed for custom Amazon client)
- `Microsoft.Extensions.Logging` (for structured logging)

### Configuration Secrets
Add to `secrets-example.json`:
```json
{
  "ExternalApis": {
    "GoogleBooks": {
      "ApiKey": "YOUR_GOOGLE_BOOKS_API_KEY_HERE"
    },
    "Amazon": {
      "AccessKey": "YOUR_AMAZON_ACCESS_KEY_HERE",
      "SecretKey": "YOUR_AMAZON_SECRET_KEY_HERE",
      "PartnerTag": "YOUR_AMAZON_ASSOCIATE_TAG_HERE"
    }
  }
}
```

### Installation Commands
```bash
# From Intellishelf.Domain directory
dotnet add package Google.Apis.Books.v1

# Restore all dependencies
dotnet restore Intellishelf.Api.sln
```

---

## Unresolved Questions for Phase 1

1. **ISBN Hyphenation**: Should we preserve original hyphenation in database, or always normalize?
   - **Decision**: Normalize (remove hyphens) for storage, add hyphens only in UI display

2. **Book Edition Handling**: ISBN-10/13 may represent different editions of same book. Duplicate check strategy?
   - **Decision**: Treat different ISBNs as different books (user may intentionally add multiple editions)

3. **Amazon API Credentials**: Requires affiliate program participation. Fallback if not available?
   - **Decision**: Make Amazon integration optional (config check), skip if credentials missing

4. **Cover Image Copyright**: Google/Amazon images may have usage restrictions. Legal review needed?
   - **Decision**: Phase 1 assumption: Fair use for personal bookshelf. Legal review before public launch

---

**Next Steps**: Proceed to Phase 1 (Data Model, Contracts, Quickstart)
