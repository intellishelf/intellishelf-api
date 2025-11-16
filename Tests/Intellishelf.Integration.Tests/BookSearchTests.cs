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
        var prideAndPrejudice = CreateBookEntity("Pride and Prejudice", "Jane Austen");
        var wuthering = CreateBookEntity("Wuthering Heights", "Emily Brontë");
        var frankenstein = CreateBookEntity("Frankenstein", "Mary Shelley");

        await _mongoDbFixture.SeedBooksAndWaitForIndexing(prideAndPrejudice, wuthering, frankenstein);

        // Act
        var response = await _client.GetAsync("/api/books/search?searchTerm=Prejudice");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Book>>();
        Assert.NotNull(pagedResult);
        Assert.Single(pagedResult.Items);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal("Pride and Prejudice", pagedResult.Items.First().Title);
    }

    [Fact]
    private async Task GivenBooksWithMatchingAuthor_WhenSearch_ThenReturnsMatchingBooks()
    {
        // Arrange
        var greatExpectations = CreateBookEntity("Great Expectations", "Charles Dickens");
        var atale = CreateBookEntity("A Tale of Two Cities", "Charles Dickens");
        var mobyDick = CreateBookEntity("Moby Dick", "Herman Melville");

        await _mongoDbFixture.SeedBooksAndWaitForIndexing(greatExpectations, atale, mobyDick);

        // Act
        var response = await _client.GetAsync("/api/books/search?searchTerm=Dickens");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Book>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(2, pagedResult.Items.Count);
        Assert.Equal(2, pagedResult.TotalCount);
        var titles = pagedResult.Items.Select(b => b.Title).ToArray();
        Assert.Contains("Great Expectations", titles);
        Assert.Contains("A Tale of Two Cities", titles);
    }

    [Fact]
    private async Task GivenMultipleMatches_WhenSearch_ThenReturnsAllMatches()
    {
        // Arrange
        var theBronteSisters = CreateBookEntity("The Brontë Sisters", "Various");
        var jane = CreateBookEntity("Jane Eyre", "Charlotte Brontë");
        var wuthering = CreateBookEntity("Wuthering Heights", "Emily Brontë");

        await _mongoDbFixture.SeedBooksAndWaitForIndexing(theBronteSisters, jane, wuthering);

        // Act - Search for "Brontë" which appears in 3 books
        var response = await _client.GetAsync("/api/books/search?searchTerm=Brontë");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Book>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(3, pagedResult.Items.Count);
        Assert.Equal(3, pagedResult.TotalCount);

        var titles = pagedResult.Items.Select(b => b.Title).ToArray();
        Assert.Contains("The Brontë Sisters", titles);
        Assert.Contains("Jane Eyre", titles);
        Assert.Contains("Wuthering Heights", titles);
    }

    [Fact]
    private async Task GivenBooks_WhenSearchWithPagination_ThenReturnsPagedResults()
    {
        // Arrange
        var sherlock1 = CreateBookEntity("A Scandal in Bohemia", "Arthur Conan Doyle");
        var sherlock2 = CreateBookEntity("The Speckled Band", "Arthur Conan Doyle");
        var sherlock3 = CreateBookEntity("The Red-Headed League", "Arthur Conan Doyle");

        await _mongoDbFixture.SeedBooksAndWaitForIndexing(sherlock1, sherlock2, sherlock3);

        // Act - Search with page size of 2
        var response = await _client.GetAsync("/api/books/search?searchTerm=Doyle&page=1&pageSize=2");

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
            UserId = ObjectId.Parse(DefaultTestUsers.Authenticated.Id),
            Status = ReadingStatus.Unread
        };
    }

    public void Dispose() => _factory.Dispose();

    public Task DisposeAsync() => Task.CompletedTask;
}