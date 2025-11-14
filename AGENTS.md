# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

This codebase uses **railway-oriented programming** instead of exceptions for expected errors:

```csharp
// Service methods return TryResult<T>
public async Task<TryResult<Book>> TryGetBookAsync(string userId, string bookId) =>
    await bookDao.GetBookAsync(userId, bookId);

// TryResult has two states: Success with value, or Error
public record TryResult<TResult> : TryResult
{
    [MemberNotNullWhen(false, nameof(Error))]
    public virtual bool IsSuccess { get; protected init; }
    public Error? Error { get; }
    public TryResult? Value { get; }
}

// Errors are simple records with Code and Message
public record Error(string Code, string Message);
```

**How to use in controllers:**
```csharp
var result = await bookService.TryGetBookAsync(userId, bookId);
if (!result.IsSuccess)
    return HandleErrorResponse(result.Error);  // Converts to ProblemDetails
return Ok(result.Value);
```

**Implicit conversions allow clean DAO/Service implementations:**
```csharp
// Return an error directly
if (book == null)
    return new Error(BookErrorCodes.BookNotFound, "Book not found.");

// Return a value (implicitly converted to TryResult<Book>)
return mapper.Map(bookEntity);

// Return void success
return TryResult.Success();
```

**Error Codes:** Defined in static classes per domain:
- `src/Intellishelf.Domain/Books/Errors/BookErrorCodes.cs`
- `src/Intellishelf.Domain/Users/Models/UserErrorCodes.cs`
- `src/Intellishelf.Domain/Files/ErrorCodes/FileErrorCodes.cs`
- `src/Intellishelf.Domain/Ai/Errors/AiErrorCodes.cs`

## Error to HTTP Status Mapping

**Location:** `src/Intellishelf.Api/Controllers/ApiControllerBase.cs`

Error codes map to HTTP status codes in `ApiControllerBase.MapErrorToStatusCode()`:

```csharp
private static int MapErrorToStatusCode(string code) => code switch
{
    BookErrorCodes.BookNotFound => StatusCodes.Status404NotFound,
    UserErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
    UserErrorCodes.AlreadyExists => StatusCodes.Status409Conflict,
    FileErrorCodes.InvalidFileType => StatusCodes.Status400BadRequest,
    _ => StatusCodes.Status500InternalServerError
};
```

**When adding new error codes:**
1. Add constant to appropriate `*ErrorCodes` class
2. Update `MapErrorToStatusCode()` switch expression
3. Use the error code in DAO/Service: `return new Error(YourErrorCodes.NewError, "Message");`

## Module System (Dependency Injection)

**Location:** `src/Intellishelf.Api/Modules/`

Instead of massive `Program.cs`, DI is organized into modules per feature:

- **DbModule:** MongoDB configuration and IMongoDatabase singleton
- **UsersModule:** Authentication (JWT + Cookie + Google OAuth), user services, background cleanup
- **BooksModule:** Book services, DAOs, mappers, image processors
- **AzureModule:** Azure Blob Storage client
- **AiModule:** OpenAI ChatClient configuration

**Pattern:**
```csharp
// In Program.cs
DbModule.Register(builder);
UsersModule.Register(builder);
BooksModule.Register(builder.Services);
// ...

// In each module
public static class BooksModule
{
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<IBookEntityMapper, BookEntityMapper>();  // Stateless
        services.AddTransient<IBookDao, BookDao>();                    // Per-request
        services.AddTransient<IBookService, BookService>();
    }
}
```

**Lifecycle rules:**
- `AddSingleton`: Mappers, validators, processors (stateless, thread-safe)
- `AddTransient`: DAOs, Services (new instance per injection)
- `AddScoped`: Services that need per-request state (e.g., FileStorageService)

## Layer Communication Flow

```
HTTP Request
    ↓
[Authentication Middleware] → Validates JWT/Cookie/Google token
    ↓
[BooksController] → Extracts UserId from User.Claims
    ↓
[BookService] → Orchestrates business logic (e.g., delete old image before update)
    ↓
[BookDao] → Executes MongoDB queries with Builders API
    ↓
[MongoDB] → Returns BookEntity
    ↓
[Mapper] → BookEntity → Book (domain model)
    ↓
[TryResult<Book>] → Flows back up
    ↓
[Controller] → HandleErrorResponse() or Ok()
    ↓
ProblemDetails or JSON response
```

**Key point:** Services coordinate cross-cutting concerns (e.g., deleting files), DAOs focus purely on data access.

## Integration Tests with Testcontainers

**Location:** `tests/Intellishelf.Integration.Tests/`

Integration tests use **real MongoDB and Azure Storage containers** (not mocks):

```csharp
// Fixtures spin up containers
public class MongoDbFixture : IAsyncLifetime
{
    private MongoDbContainer _mongoContainer;
    public string ConnectionString => _mongoContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder().WithImage("mongo:7.0").Build();
        await _mongoContainer.StartAsync();
    }

    // Helper methods for seeding test data
    public Task SeedDefaultUserAsync() => ...;
    public Task SeedBooksAsync(params BookEntity[] books) => ...;
    public Task ClearBooksAsync() => ...;
}
```

**Test structure:**
```csharp
[Collection("Integration Tests")]  // Share fixtures across tests in collection
public sealed class BooksTests : IAsyncLifetime
{
    private readonly HttpClient _client;

    public BooksTests(MongoDbFixture mongoDbFixture, AzuriteFixture azuriteFixture)
    {
        var factory = new TestWebApplicationFactory(mongoDbFixture, azuriteFixture);
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _mongoDbFixture.ClearBooksAsync();
        await _mongoDbFixture.SeedDefaultUserAsync();
    }

    [Fact]
    public async Task GivenNoBooks_WhenGetBooks_ThenEmptyPagedResult() { ... }
}
```

**TestWebApplicationFactory** swaps real MongoDB/Storage for containers and uses `TestAuthHandler` to bypass real authentication.

**Requirements:**
- Docker must be running for integration tests
- Tests are isolated per collection with shared fixtures
- Use `DefaultTestUsers.Authenticated` for seeding authenticated context

## Data Access Patterns

**Interfaces in Domain, Implementations in Data:**
- Interface: `src/Intellishelf.Domain/Books/DataAccess/IBookDao.cs`
- Implementation: `src/Intellishelf.Data/Books/DataAccess/BookDao.cs`

**MongoDB specifics:**
- Uses MongoDB.Driver fluent API (`Builders<T>.Filter`, `Builders<T>.Update`, `Builders<T>.Sort`)
- ObjectId for user IDs, string IDs for entities
- Entity classes inherit from `EntityBase` with `[BsonId]` attribute
- Collections accessed via injected `IMongoDatabase`

**Example DAO pattern:**
```csharp
public class BookDao(IMongoDatabase database, IBookEntityMapper mapper) : IBookDao
{
    private readonly IMongoCollection<BookEntity> _booksCollection =
        database.GetCollection<BookEntity>(BookEntity.CollectionName);

    public async Task<TryResult<Book>> GetBookAsync(string userId, string bookId)
    {
        var filter = Builders<BookEntity>.Filter.And(
            Builders<BookEntity>.Filter.Eq(b => b.UserId, ObjectId.Parse(userId)),
            Builders<BookEntity>.Filter.Eq(b => b.Id, bookId));

        var book = await _booksCollection.Find(filter).FirstOrDefaultAsync();

        if (book == null)
            return new Error(BookErrorCodes.BookNotFound, "Book not found.");

        return mapper.Map(book);  // Implicit conversion to TryResult<Book>
    }
}
```

## Authentication Architecture

**Location:** `src/Intellishelf.Api/Modules/UsersModule.cs`

The API supports **3 authentication schemes** routed by a policy scheme:

1. **JWT Bearer** (`AuthConfig.JwtScheme`): For mobile clients, tokens in `Authorization: Bearer <token>` header
2. **Cookie** (`AuthConfig.CookieScheme`): For web clients, cookie-based sessions
3. **Google OAuth** (`GoogleDefaults.AuthenticationScheme`): External provider

**Routing logic in Program.cs:**
```csharp
.AddPolicyScheme("Custom", "JWT or Cookie", options =>
{
    options.ForwardDefaultSelector = ctx =>
    {
        var auth = ctx.Request.Headers.Authorization.ToString();
        return !string.IsNullOrEmpty(auth) ? AuthConfig.JwtScheme : AuthConfig.CookieScheme;
    };
})
```

**All API endpoints require `[Authorize]` by default** (via `ApiControllerBase`). Use `CurrentUserId` property to get authenticated user ID from claims.

## AI Integration (OpenAI)

**Location:** `src/Intellishelf.Domain/Ai/Services/AiService.cs`

Books can be parsed from OCR text using OpenAI structured outputs:

```csharp
public class AiService(ChatClient chatClient) : IAiService
{
    // Uses JSON schema to enforce structured response
    private static readonly ChatCompletionOptions ChatOptions = new()
    {
        Temperature = 0.0f,
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "book_info",
            jsonSchema: BinaryData.FromBytes(/* schema for ParsedBook */),
            jsonSchemaIsStrict: true)
    };

    public async Task<TryResult<ParsedBook>> ParseBookFromTextAsync(string text, bool useMockedAi)
    {
        if (useMockedAi)
            return new ParsedBook { Title = "Sample Book", ... };  // Mock for testing

        var response = await chatClient.CompleteChatAsync(messages, ChatOptions);
        var book = JsonSerializer.Deserialize<ParsedBook>(response.Value.Content[0].Text);

        return book ?? new Error(AiErrorCodes.RequestFailed, "Response from AI could not be parsed.");
    }
}
```

**Endpoint:** `POST /books/parse-text` accepts OCR text and returns structured book data.

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
