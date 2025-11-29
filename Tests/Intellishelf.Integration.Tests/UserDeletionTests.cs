using System.Net;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using MongoDB.Bson;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class UserDeletionTests : IAsyncLifetime, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbFixture _mongoDbFixture;
    private readonly AzuriteFixture _azuriteFixture;

    public UserDeletionTests(MongoDbFixture mongoDbFixture, AzuriteFixture azuriteFixture)
    {
        _factory = new TestWebApplicationFactory(mongoDbFixture, azuriteFixture);
        _client = _factory.CreateClient();
        _mongoDbFixture = mongoDbFixture;
        _azuriteFixture = azuriteFixture;
    }

    public async Task InitializeAsync()
    {
        await _mongoDbFixture.ClearBooksAsync();
        await _mongoDbFixture.ClearUsersAsync();
        await _mongoDbFixture.ClearRefreshTokensAsync();
        await _mongoDbFixture.SeedDefaultUserAsync();
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenDeleteAccount_ThenAllDataRemoved()
    {
        // Arrange
        var userId = DefaultTestUsers.Authenticated.Id;

        // Seed books with cover images
        var coverUrl1 = await _azuriteFixture.SeedBlobAsync(
            $"userFiles/{userId}/cover1.jpg",
            "cover1"u8.ToArray());
        var coverUrl2 = await _azuriteFixture.SeedBlobAsync(
            $"userFiles/{userId}/cover2.jpg",
            "cover2"u8.ToArray());

        var book1 = CreateBookEntity("Book 1", "Author 1", coverImageUrl: coverUrl1);
        var book2 = CreateBookEntity("Book 2", "Author 2", coverImageUrl: coverUrl2);
        var book3 = CreateBookEntity("Book 3", "Author 3");

        await _mongoDbFixture.SeedBooksAsync(book1, book2, book3);
        await _mongoDbFixture.SeedRefreshTokenAsync(userId, "test-token-1");

        // Act
        var response = await _client.DeleteAsync("/api/auth/account");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify all data deleted
        var userExists = await _mongoDbFixture.UserExistsAsync(userId);
        Assert.False(userExists);

        var books = await _mongoDbFixture.GetBooksByUserIdAsync(userId);
        Assert.Empty(books);

        var tokens = await _mongoDbFixture.GetRefreshTokensByUserIdAsync(userId);
        Assert.Empty(tokens);

        var blob1Exists = await _azuriteFixture.BlobExistsFromUrlAsync(coverUrl1);
        var blob2Exists = await _azuriteFixture.BlobExistsFromUrlAsync(coverUrl2);
        Assert.False(blob1Exists);
        Assert.False(blob2Exists);
    }

    [Fact]
    public async Task GivenUserWithNoBooksOrFiles_WhenDeleteAccount_ThenUserDeleted()
    {
        // Arrange - user exists but has no books or files
        var userId = DefaultTestUsers.Authenticated.Id;

        // Act
        var response = await _client.DeleteAsync("/api/auth/account");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var userExists = await _mongoDbFixture.UserExistsAsync(userId);
        Assert.False(userExists);
    }

    [Fact]
    public async Task GivenUserWithManyBooks_WhenDeleteAccount_ThenAllBooksDeleted()
    {
        // Arrange
        var userId = DefaultTestUsers.Authenticated.Id;
        var books = Enumerable.Range(1, 20)
            .Select(i => CreateBookEntity($"Book {i}", $"Author {i}"))
            .ToArray();

        await _mongoDbFixture.SeedBooksAsync(books);

        // Act
        var response = await _client.DeleteAsync("/api/auth/account");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remainingBooks = await _mongoDbFixture.GetBooksByUserIdAsync(userId);
        Assert.Empty(remainingBooks);
    }

    [Fact]
    public async Task GivenUserWithMultipleTokens_WhenDeleteAccount_ThenAllTokensDeleted()
    {
        // Arrange
        var userId = DefaultTestUsers.Authenticated.Id;
        await _mongoDbFixture.SeedRefreshTokenAsync(userId, "token-1");
        await _mongoDbFixture.SeedRefreshTokenAsync(userId, "token-2");
        await _mongoDbFixture.SeedRefreshTokenAsync(userId, "token-3");

        // Act
        var response = await _client.DeleteAsync("/api/auth/account");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var tokens = await _mongoDbFixture.GetRefreshTokensByUserIdAsync(userId);
        Assert.Empty(tokens);
    }

    [Fact]
    public async Task GivenUserWithMixedData_WhenDeleteAccount_ThenCompleteRemoval()
    {
        // Arrange
        var userId = DefaultTestUsers.Authenticated.Id;

        // Add books with and without images
        var bookWithImage = CreateBookEntity("Book with Image", "Author 1",
            coverImageUrl: await _azuriteFixture.SeedBlobAsync(
                $"userFiles/{userId}/book-image.jpg",
                "content"u8.ToArray()));

        var bookWithoutImage = CreateBookEntity("Book without Image", "Author 2");

        await _mongoDbFixture.SeedBooksAsync(bookWithImage, bookWithoutImage);

        // Add multiple tokens
        for (int i = 0; i < 5; i++)
        {
            await _mongoDbFixture.SeedRefreshTokenAsync(userId, $"token-{i}");
        }

        // Act
        var response = await _client.DeleteAsync("/api/auth/account");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var userExists = await _mongoDbFixture.UserExistsAsync(userId);
        Assert.False(userExists);

        var books = await _mongoDbFixture.GetBooksByUserIdAsync(userId);
        Assert.Empty(books);

        var tokens = await _mongoDbFixture.GetRefreshTokensByUserIdAsync(userId);
        Assert.Empty(tokens);
    }

    private static BookEntity CreateBookEntity(
        string title,
        string author,
        string? userId = null,
        string? coverImageUrl = null)
    {
        var timestamp = DateTime.UtcNow;
        return new BookEntity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Title = title,
            Authors = [author],
            CreatedDate = timestamp,
            ModifiedDate = timestamp,
            UserId = ObjectId.Parse(userId ?? DefaultTestUsers.Authenticated.Id),
            CoverImageUrl = coverImageUrl,
            Status = ReadingStatus.Unread
        };
    }

    public void Dispose() => _factory.Dispose();
    public Task DisposeAsync() => Task.CompletedTask;
}
