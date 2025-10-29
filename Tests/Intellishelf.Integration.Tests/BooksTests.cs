using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Intellishelf.Common.TryResult;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Files.ErrorCodes;
using Intellishelf.Domain.Files.Services;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class BooksTests : IAsyncLifetime, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbFixture _mongoDbFixture;
    private readonly AzuriteFixture _azuriteFixture;
    private static readonly byte[] SampleImageBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Infra", "Fixtures", "sample.jpg"));

    public BooksTests(MongoDbFixture mongoDbFixture, AzuriteFixture azuriteFixture)
    {
        _factory = new TestWebApplicationFactory(mongoDbFixture, azuriteFixture);
        _client = _factory.CreateClient();
        _mongoDbFixture = mongoDbFixture;
        _azuriteFixture = azuriteFixture;
    }

    public async Task InitializeAsync()
    {
        await _mongoDbFixture.ClearBooksAsync();
        await _mongoDbFixture.SeedDefaultUserAsync();
    }

    [Fact]
    private async Task GivenNoBooks_WhenGetBooks_ThenEmptyPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/books");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = Assert.IsType<PagedResult<Book>>(await response.Content.ReadFromJsonAsync<PagedResult<Book>>());
        Assert.Empty(pagedResult.Items);
        Assert.Equal(0, pagedResult.TotalCount);
    }

    [Fact]
    private async Task GivenExistingBooks_WhenGetBooks_ThenReturnsPagedResult()
    {
        // Arrange
        var firstBook = CreateBookEntity("First", "Alice");
        var secondBook = CreateBookEntity("Second", "Bob");
        var thirdBook = CreateBookEntity("Third", "Carol");

        await _mongoDbFixture.SeedBooksAsync(firstBook, secondBook, thirdBook);

        // Act
        var response = await _client.GetAsync("/api/books?page=1&pageSize=2&orderBy=Added&ascending=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = Assert.IsType<PagedResult<Book>>(await response.Content.ReadFromJsonAsync<PagedResult<Book>>());
        Assert.Equal(3, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count);
        Assert.Equal(1, pagedResult.Page);
        Assert.Equal(2, pagedResult.PageSize);
        Assert.All(pagedResult.Items, b => Assert.Equal(DefaultTestUsers.Authenticated.Id, b.UserId));
    }

    [Fact]
    private async Task GivenMultipleBooks_WhenGetBooksSecondPageDescending_ThenReturnsExpectedSlice()
    {
        // Arrange
        var books = Enumerable
            .Range(1, 5)
            .Select(index => CreateBookEntity(
                $"Book {index}",
                $"Author {index}",
                createdDate: new DateTime(2024, 1, index)))
            .ToArray();

        await _mongoDbFixture.SeedBooksAsync(books);

        // Act
        var response = await _client.GetAsync("/api/books?page=2&pageSize=2&orderBy=Added&ascending=false");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = Assert.IsType<PagedResult<Book>>(await response.Content.ReadFromJsonAsync<PagedResult<Book>>());
        Assert.Equal(5, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count);
        Assert.Equal(2, pagedResult.Page);
        Assert.Equal(2, pagedResult.PageSize);

        var titles = pagedResult.Items.Select(b => b.Title).ToArray();
        Assert.Equal(new[] { "Book 3", "Book 2" }, titles);
    }

    [Fact]
    private async Task GivenNoBooks_WhenGetAllBooks_ThenEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/books/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var books = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<Book>>();
        Assert.NotNull(books);
        Assert.Empty(books);
    }

    [Fact]
    private async Task GivenExistingBooks_WhenGetAllBooks_ThenReturnsUserBooks()
    {
        // Arrange
        var firstBook = CreateBookEntity("First", "Alice");
        var secondBook = CreateBookEntity("Second", "Bob");
        var thirdBook = CreateBookEntity("Third", "Carol");

        await _mongoDbFixture.SeedBooksAsync(firstBook, secondBook, thirdBook);

        // Act
        var response = await _client.GetAsync("/api/books/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var books = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<Book>>();
        Assert.NotNull(books);
        Assert.Equal(3, books.Count);
        Assert.Contains(books, b => b.Title == firstBook.Title);
        Assert.Contains(books, b => b.Title == secondBook.Title);
        Assert.Contains(books, b => b.Title == thirdBook.Title);
        Assert.All(books, b => Assert.Equal(DefaultTestUsers.Authenticated.Id, b.UserId));
    }

    [Fact]
    private async Task GivenExistingBook_WhenGetBook_ThenReturnBook()
    {
        // Arrange
        var bookEntity = CreateBookEntity("Single", "Author");
        await _mongoDbFixture.SeedBooksAsync(bookEntity);

        // Act
        var response = await _client.GetAsync($"/api/books/{bookEntity.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var book = Assert.IsType<Book>(await response.Content.ReadFromJsonAsync<Book>());
        Assert.Equal(bookEntity.Id, book.Id);
        Assert.Equal(bookEntity.Title, book.Title);
        Assert.Equal(DefaultTestUsers.Authenticated.Id, book.UserId);
    }

    [Fact]
    private async Task GivenValidBookData_WhenPostNewBookWithFile_ThenCreated()
    {
        // Arrange
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent("Test Book Title"), "Title");
        content.Add(new StringContent("Test Author"), "Authors[0]");
        content.Add(new StringContent("Test description"), "Description");
        content.Add(new StringContent(DateTime.UtcNow.ToString("O")), "PublicationDate");

        var imageContent = new ByteArrayContent(SampleImageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent, "ImageFile", "test-cover.jpg");

        // Act
        var response = await _client.PostAsync("/api/books", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var book = Assert.IsType<Book>(await response.Content.ReadFromJsonAsync<Book>());

        Assert.False(string.IsNullOrWhiteSpace(book.Id));
        Assert.Equal("Test Book Title", book.Title);
        Assert.Contains("Test Author", book.Authors);
        Assert.Equal(DefaultTestUsers.Authenticated.Id, book.UserId);

        var location = response.Headers.Location;
        Assert.NotNull(location);

        var persistedBook = await _mongoDbFixture.FindBookByIdAsync(book.Id);
        Assert.NotNull(persistedBook);
        Assert.Equal("Test Book Title", persistedBook!.Title);
        Assert.Contains("Test Author", persistedBook.Authors ?? []);

        if (!string.IsNullOrWhiteSpace(persistedBook.CoverImageUrl))
        {
            var blobExists = await _azuriteFixture.BlobExistsFromUrlAsync(persistedBook.CoverImageUrl);
            Assert.True(blobExists);
        }
    }

    [Fact]
    private async Task GivenUnsupportedImageFile_WhenPostNewBook_ThenReturnsBadRequest()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Invalid Cover Book"), "Title");
        content.Add(new StringContent("Author"), "Authors[0]");

        var invalidFile = new ByteArrayContent("pdf"u8.ToArray());
        invalidFile.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(invalidFile, "ImageFile", "cover.pdf");

        // Act
        var response = await _client.PostAsync("/api/books", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal(FileErrorCodes.InvalidFileType, problem.Type);
    }

    [Fact]
    private async Task GivenExistingBook_WhenDeleteBook_ThenRemovesBookAndCoverImage()
    {
        // Arrange
        const string coverFileName = "cover.png";
        var coverPath = $"userFiles/{DefaultTestUsers.Authenticated.Id}/{coverFileName}";

        var coverUrl = await _azuriteFixture.SeedBlobAsync(coverPath, "cover data"u8.ToArray());

        var bookToDelete = CreateBookEntity(
            title: "Book to delete",
            author: "Author",
            coverImageUrl: coverUrl);

        await _mongoDbFixture.SeedBooksAsync(bookToDelete);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/books/{bookToDelete.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Null(await _mongoDbFixture.FindBookByIdAsync(bookToDelete.Id));

        var blobExists = await _azuriteFixture.BlobExistsAsync(coverPath);
        Assert.False(blobExists);
    }

    [Fact]
    private async Task GivenExistingBook_WhenUpdateBook_ThenUpdates()
    {
        // Arrange
        var book = CreateBookEntity("Original", "Mallory");
        await _mongoDbFixture.SeedBooksAsync(book);

        using var updatedBook = new MultipartFormDataContent();
        updatedBook.Add(new StringContent("Updated Title"), "Title");
        updatedBook.Add(new StringContent("Author Two"), "Authors[0]");

        // Act
        var updateResponse = await _client.PutAsync($"/api/books/{book.Id}", updatedBook);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var storedBook = await _mongoDbFixture.FindBookByIdAsync(book.Id);
        Assert.NotNull(storedBook);
        Assert.Equal("Updated Title", storedBook.Title);
        Assert.Contains("Author Two", storedBook.Authors ?? []);
    }

    [Fact]
    private async Task GivenBookOwnedByAnotherUser_WhenDeleteBook_ThenNotFound()
    {
        // Arrange
        var foreignUserId = ObjectId.GenerateNewId().ToString();
        var foreignBook = CreateBookEntity("Foreign", "Mallory", foreignUserId);
        await _mongoDbFixture.SeedBooksAsync(foreignBook);

        // Act
        var response = await _client.DeleteAsync($"/api/books/{foreignBook.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal(BookErrorCodes.BookNotFound, problem.Type);
    }

    [Fact]
    private async Task GivenFileUploadFailure_WhenPostNewBook_ThenReturnsProblem()
    {
        // Arrange
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService>(new FailingFileStorageService());
            });
        });

        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Failure book"), "Title");
        content.Add(new StringContent("Author"), "Authors[0]");

        var imageContent = new ByteArrayContent(SampleImageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent, "ImageFile", "bad.jpg");

        // Act
        var response = await client.PostAsync("/api/books", content);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal(FileErrorCodes.UploadFailed, problem.Type);
    }

    private static BookEntity CreateBookEntity(
        string title,
        string author,
        string? userId = null,
        DateTime? createdDate = null,
        string? coverImageUrl = null)
    {
        var timestamp = createdDate ?? DateTime.UtcNow;
        return new BookEntity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Title = title,
            Authors = [author],
            CreatedDate = timestamp,
            ModifiedDate = timestamp,
            UserId = ObjectId.Parse(userId ?? DefaultTestUsers.Authenticated.Id),
            CoverImageUrl = coverImageUrl
        };
    }

    public void Dispose() => _factory.Dispose();

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed class FailingFileStorageService : IFileStorageService
    {
        public Task<TryResult<string>> UploadFileAsync(string userId, Stream fileStream, string fileName)
        {
            return Task.FromResult<TryResult<string>>(new Error(FileErrorCodes.UploadFailed, "Upload failed"));
        }

        public Task<TryResult<bool>> DeleteFileFromUrlAsync(string url) => Task.FromResult<TryResult<bool>>(false);
    }
}