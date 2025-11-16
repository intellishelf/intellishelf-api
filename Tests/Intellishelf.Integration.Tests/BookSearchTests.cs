using System.Net;
using System.Net.Http.Json;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using MongoDB.Bson;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class BookSearchTests : IAsyncLifetime, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbFixture _mongoDbFixture;

    public BookSearchTests(MongoDbFixture mongoDbFixture, AzuriteFixture azuriteFixture)
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

    [Fact]
    private async Task GivenNoBooks_WhenSearch_ThenReturnsEmptyResult()
    {
        // No seeding needed - testing empty state

        // Act
        var response = await _client.GetAsync("/api/books/search?searchTerm=test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Book>>();
        Assert.NotNull(pagedResult);
        Assert.Empty(pagedResult.Items);
        Assert.Equal(0, pagedResult.TotalCount);
    }

    [Fact]
    private async Task GivenBooksWithMatchingTitle_WhenSearch_ThenReturnsMatchingBooks()
    {
        // Arrange
        var csharpBook = CreateBookEntity("The C# Programming Language", "Anders Hejlsberg");
        var pythonBook = CreateBookEntity("Learning Python", "Mark Lutz");
        var javaBook = CreateBookEntity("Effective Java", "Joshua Bloch");

        await _mongoDbFixture.SeedBooksAndWaitForIndexing(csharpBook, pythonBook, javaBook);

        // Act
        var response = await _client.GetAsync("/api/books/search?searchTerm=Python");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Book>>();
        Assert.NotNull(pagedResult);
        Assert.Single(pagedResult.Items);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal("Learning Python", pagedResult.Items.First().Title);
    }

    [Fact]
    private async Task GivenBooksWithMatchingAuthor_WhenSearch_ThenReturnsMatchingBooks()
    {
        // Arrange
        var csharpBook = CreateBookEntity("C# in Depth", "Jon Skeet");
        var pythonBook = CreateBookEntity("Fluent Python", "Luciano Ramalho");
        var anotherCsharpBook = CreateBookEntity("Pro C#", "Andrew Troelsen");

        await _mongoDbFixture.SeedBooksAndWaitForIndexing(csharpBook, pythonBook, anotherCsharpBook);

        // Act
        var response = await _client.GetAsync("/api/books/search?searchTerm=Skeet");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Book>>();
        Assert.NotNull(pagedResult);
        Assert.Single(pagedResult.Items);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal("C# in Depth", pagedResult.Items.First().Title);
        Assert.Contains("Jon Skeet", pagedResult.Items.First().Authors);
    }

    [Fact]
    private async Task GivenMultipleMatches_WhenSearch_ThenReturnsAllMatches()
    {
        // Arrange
        var designPatternsBook = CreateBookEntity("Design Patterns", "Gang of Four");
        var cleanCodeBook = CreateBookEntity("Clean Code", "Robert Martin");
        var refactoringBook = CreateBookEntity("Refactoring", "Martin Fowler");

        await _mongoDbFixture.SeedBooksAndWaitForIndexing(designPatternsBook, cleanCodeBook, refactoringBook);

        // Act - Search for "Martin" which appears in 2 books
        var response = await _client.GetAsync("/api/books/search?searchTerm=Martin");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Book>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(2, pagedResult.Items.Count);
        Assert.Equal(2, pagedResult.TotalCount);

        var titles = pagedResult.Items.Select(b => b.Title).ToArray();
        Assert.Contains("Clean Code", titles);
        Assert.Contains("Refactoring", titles);
    }

    [Fact]
    private async Task GivenBooks_WhenSearchWithPagination_ThenReturnsPagedResults()
    {
        // Arrange
        var book1 = CreateBookEntity("Programming Book One", "Author A");
        var book2 = CreateBookEntity("Programming Book Two", "Author B");
        var book3 = CreateBookEntity("Programming Book Three", "Author C");

        await _mongoDbFixture.SeedBooksAndWaitForIndexing(book1, book2, book3);

        // Act - Search with page size of 2
        var response = await _client.GetAsync("/api/books/search?searchTerm=Programming&page=1&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Book>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(2, pagedResult.Items.Count); // Page size is 2
        Assert.Equal(3, pagedResult.TotalCount); // Total matching books is 3
        Assert.Equal(1, pagedResult.Page);
        Assert.Equal(2, pagedResult.PageSize);
    }

    private static BookEntity CreateBookEntity(string title, string author)
    {
        var timestamp = DateTime.UtcNow;
        return new BookEntity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Title = title,
            Authors = [author],
            CreatedDate = timestamp,
            ModifiedDate = timestamp,
            UserId = ObjectId.Parse(DefaultTestUsers.Authenticated.Id)
        };
    }

    public void Dispose() => _factory.Dispose();

    public Task DisposeAsync() => Task.CompletedTask;
}
