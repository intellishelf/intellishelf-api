using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class BooksTests : IAsyncLifetime, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbFixture _mongoDbFixture;

    public BooksTests(MongoDbFixture mongoDbFixture, AzuriteFixture azuriteFixture)
    {
        _factory = new TestWebApplicationFactory(mongoDbFixture, azuriteFixture);
        _client = _factory.CreateClient();
        _mongoDbFixture = mongoDbFixture;
    }

    public async Task InitializeAsync()
    {
        await _mongoDbFixture.ClearBooksAsync();
        await _mongoDbFixture.SeedDefaultUserAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

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
        var response = await _client.GetAsync("/api/books?page=1&pageSize=2&orderBy=Added&ascending=false");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = Assert.IsType<PagedResult<Book>>(await response.Content.ReadFromJsonAsync<PagedResult<Book>>());
        Assert.Equal(3, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count);
        Assert.All(pagedResult.Items, b => Assert.Equal(DefaultTestUsers.Authenticated.Id, b.UserId));
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

        var endpoint = $"/api/books/{bookEntity.Id}";

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var book = Assert.IsType<Book>(await response.Content.ReadFromJsonAsync<Book>());
        Assert.Equal(bookEntity.Id, book.Id);
        Assert.Equal(bookEntity.Title, book.Title);
        Assert.Equal(DefaultTestUsers.Authenticated.Id, book.UserId);
    }

    [Fact]
    private async Task GivenBookOwnedByAnotherUser_WhenGetBook_ThenUnauthorized()
    {
        // Arrange
        var anotherUserId = ObjectId.GenerateNewId().ToString();
        var foreignBook = CreateBookEntity("Foreign", "Mallory", anotherUserId);
        await _mongoDbFixture.SeedBooksAsync(foreignBook);

        var endpoint = $"/api/books/{foreignBook.Id}";

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal(BookErrorCodes.AccessDenied, problem.Type);
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

        var imageContent = new ByteArrayContent("fake image data"u8.ToArray());
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

        var followUp = await _client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, followUp.StatusCode);

        var fetchedBook = Assert.IsType<Book>(await followUp.Content.ReadFromJsonAsync<Book>());
        Assert.Equal(book.Id, fetchedBook.Id);
        Assert.Equal(book.Title, fetchedBook.Title);
        Assert.Equal(book.UserId, fetchedBook.UserId);
    }

    private static BookEntity CreateBookEntity(string title, string author, string? userId = null)
    {
        return new BookEntity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Title = title,
            Authors = [author],
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            UserId = userId ?? DefaultTestUsers.Authenticated.Id
        };
    }


    public void Dispose() => _factory.Dispose();
}
