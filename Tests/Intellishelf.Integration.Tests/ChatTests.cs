using System.Net;
using System.Net.Http.Json;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Chat.Models;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class ChatTests : IAsyncLifetime, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbFixture _mongoDbFixture;

    public ChatTests(MongoDbFixture mongoDbFixture, AzuriteFixture azuriteFixture)
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
    private async Task GivenEmptyConversation_WhenSendMessage_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new ChatRequest(new List<ChatMessage>());

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/message", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Chat.EmptyConversation", problem.Type);
    }

    [Fact]
    private async Task GivenValidMessage_WhenNoBooks_ThenReturnsSuccessWithZeroBooks()
    {
        // Arrange
        var request = new ChatRequest(new List<ChatMessage>
        {
            new("user", "What books do I have?")
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/message", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(chatResponse);
        Assert.NotEmpty(chatResponse.Message);
        Assert.Equal(0, chatResponse.BooksContextCount);
    }

    [Fact]
    private async Task GivenValidMessage_WhenBooksExist_ThenReturnsSuccessWithBooksContext()
    {
        // Arrange
        await _mongoDbFixture.SeedBooksAsync(
            CreateBookEntity("Clean Code", "Robert C. Martin"),
            CreateBookEntity("The Pragmatic Programmer", "Andrew Hunt, David Thomas")
        );

        var request = new ChatRequest(new List<ChatMessage>
        {
            new("user", "What books do I have in my library?")
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/message", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(chatResponse);
        Assert.NotEmpty(chatResponse.Message);
        Assert.Equal(2, chatResponse.BooksContextCount);
    }

    [Fact]
    private async Task GivenMultiTurnConversation_WhenSendMessage_ThenMaintainsContext()
    {
        // Arrange
        await _mongoDbFixture.SeedBooksAsync(
            CreateBookEntity("Clean Code", "Robert C. Martin", status: ReadingStatus.Finished),
            CreateBookEntity("The Pragmatic Programmer", "Andrew Hunt", status: ReadingStatus.ToRead)
        );

        var request = new ChatRequest(new List<ChatMessage>
        {
            new("user", "What books do I have?"),
            new("assistant", "You have 2 books: Clean Code by Robert C. Martin and The Pragmatic Programmer by Andrew Hunt."),
            new("user", "Which ones have I finished reading?")
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/message", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(chatResponse);
        Assert.NotEmpty(chatResponse.Message);
        Assert.Equal(2, chatResponse.BooksContextCount);
    }

    [Fact]
    private async Task GivenUnauthenticatedRequest_WhenSendMessage_ThenReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateUnauthenticatedClient();
        var request = new ChatRequest(new List<ChatMessage>
        {
            new("user", "What books do I have?")
        });

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/chat/message", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static BookEntity CreateBookEntity(
        string title,
        string authors,
        ReadingStatus status = ReadingStatus.ToRead)
    {
        return new BookEntity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = ObjectId.Parse(DefaultTestUsers.Authenticated.Id),
            CreatedDate = DateTime.UtcNow,
            Title = title,
            Authors = authors,
            Status = status
        };
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
