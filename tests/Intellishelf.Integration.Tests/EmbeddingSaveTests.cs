using System.Collections.Concurrent;
using System.Net;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.Models;
using Microsoft.Extensions.Logging;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Books.Services;
using Intellishelf.Domain.Explore;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class EmbeddingSaveTests(MongoDbFixture mongo, AzuriteFixture azurite) : IAsyncLifetime
{
    private readonly TestProvider _provider = new();
    private readonly CapturingLoggerProvider _logs = new();
    private WebApplicationFactory<Program> _factory = null!;
    private IBookService _books = null!;
    private string UserId => DefaultTestUsers.Authenticated.Id;

    public async ValueTask InitializeAsync()
    {
        await mongo.ClearBooksAsync();
        await mongo.SeedDefaultUserAsync();
        _factory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(_logs)).ConfigureServices(services =>
            {
                services.RemoveAll<IBookMetadataService>();
                services.AddSingleton<IBookMetadataService, TestMetadata>();
                services.RemoveAll<IEmbeddingProvider>();
                services.AddSingleton<IEmbeddingProvider>(_provider);
            }));
        _books = _factory.Services.GetRequiredService<IBookService>();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    private Task SetPro(bool pro) => mongo.Database.GetCollection<UserEntity>(UserEntity.CollectionName)
        .UpdateOneAsync(u => u.Id == UserId, Builders<UserEntity>.Update.Set(u => u.IsPro, pro));
    private AddBookRequest Add(string annotation = "first") => new() { UserId = UserId, Title = "Synthetic", Annotation = annotation, Status = ReadingStatus.Unread };
    private UpdateBookRequest Edit(string id, string annotation = "first", ReadingStatus status = ReadingStatus.Unread) => new() { UserId = UserId, Id = id, Title = "Synthetic", Annotation = annotation, Status = status };

    [Fact]
    public async Task SaveKeepsMatchingVectorAndRemovesStaleVectorOnFailureOrDowngrade()
    {
        var nonPro = await _books.TryAddBookAsync(Add());
        Assert.True(nonPro.IsSuccess);
        Assert.Equal(0, _provider.Calls);
        await SetPro(true);
        Assert.True((await _books.TryUpdateBookAsync(Edit(nonPro.Value.Id))).IsSuccess);
        var indexed = await mongo.FindBookByIdAsync(nonPro.Value.Id);
        Assert.NotNull(indexed!.Embedding);
        Assert.Equal(1536, indexed.Embedding.Length);
        Assert.Equal("test-v1", indexed.EmbeddingModel);
        Assert.Equal(1, _provider.Calls);
        Assert.True((await _books.TryUpdateBookAsync(Edit(nonPro.Value.Id, status: ReadingStatus.Read))).IsSuccess);
        Assert.Equal(1, _provider.Calls);
        Assert.Equal(indexed.EmbeddingHash, (await mongo.FindBookByIdAsync(nonPro.Value.Id))!.EmbeddingHash);
        Assert.True((await _books.TryUpdateBookAsync(Edit(nonPro.Value.Id, "changed"))).IsSuccess);
        Assert.Equal(2, _provider.Calls);
        Assert.NotEqual(indexed.EmbeddingHash, (await mongo.FindBookByIdAsync(nonPro.Value.Id))!.EmbeddingHash);
        _provider.Fail = true;
        Assert.True((await _books.TryUpdateBookAsync(Edit(nonPro.Value.Id, "again"))).IsSuccess);
        Assert.Null((await mongo.FindBookByIdAsync(nonPro.Value.Id))!.Embedding);
        _provider.Fail = false;
        Assert.True((await _books.TryUpdateBookAsync(Edit(nonPro.Value.Id, "again"))).IsSuccess);
        await SetPro(false);
        Assert.True((await _books.TryUpdateBookAsync(Edit(nonPro.Value.Id, "downgraded"))).IsSuccess);
        Assert.Null((await mongo.FindBookByIdAsync(nonPro.Value.Id))!.Embedding);
        Assert.Equal(4, _provider.Calls);
    }

    [Fact]
    public async Task NonProIsbnAddAndEditNeverCallProvider()
    {
        var isbnBook = await _books.TryAddBookFromIsbnAsync(UserId, "9780140328721");
        Assert.True(isbnBook.IsSuccess);
        Assert.Equal(0, _provider.Calls);
        Assert.Null((await mongo.FindBookByIdAsync(isbnBook.Value.Id))!.Embedding);
        var edit = await _books.TryUpdateBookAsync(Edit(isbnBook.Value.Id, "changed"));
        Assert.True(edit.IsSuccess);
        Assert.Equal(0, _provider.Calls);
        Assert.Null((await mongo.FindBookByIdAsync(isbnBook.Value.Id))!.Embedding);
        await SetPro(true);
        var proBook = await _books.TryAddBookFromIsbnAsync(UserId, "9780306406157");
        Assert.True(proBook.IsSuccess);
        Assert.Equal(1, _provider.Calls);
        Assert.NotNull((await mongo.FindBookByIdAsync(proBook.Value.Id))!.Embedding);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("error")]
    [InlineData("throw")]
    public async Task FailedUserLookupStillSavesWithoutProvider(string mode)
    {
        using var factory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserDao>();
                services.AddSingleton<IUserDao>(new FailedUserDao(mode));
                services.RemoveAll<IEmbeddingProvider>();
                services.AddSingleton<IEmbeddingProvider>(_provider);
            }));
        var books = factory.Services.GetRequiredService<IBookService>();
        var result = await books.TryAddBookAsync(Add());
        Assert.True(result.IsSuccess);
        Assert.Equal(0, _provider.Calls);
        Assert.Null((await mongo.FindBookByIdAsync(result.Value.Id))!.Embedding);
    }

    [Fact]
    public async Task ProviderExceptionAndAnnotationDoNotLeakIntoUpdateResponseOrLogs()
    {
        const string sentinel = "synthetic-private-sentinel";
        await SetPro(true);
        var book = await _books.TryAddBookAsync(Add());
        Assert.True(book.IsSuccess);
        _provider.ExceptionMessage = sentinel;
        _provider.Fail = true;
        using var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Synthetic"), "Title");
        form.Add(new StringContent(sentinel), "Annotation");
        form.Add(new StringContent(nameof(ReadingStatus.Unread)), "Status");
        using var response = await client.PutAsync($"/api/books/{book.Value.Id}", form, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(sentinel, body);
        Assert.Null((await mongo.FindBookByIdAsync(book.Value.Id))!.Embedding);
        Assert.NotEmpty(_logs.Entries);
        Assert.DoesNotContain(_logs.Entries, entry => entry.Contains(sentinel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ForeignEditCannotCallProvider()
    {
        await SetPro(true);
        var book = await _books.TryAddBookAsync(Add());
        Assert.True(book.IsSuccess);
        _provider.Calls = 0;
        var result = await _books.TryUpdateBookAsync(new UpdateBookRequest { UserId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Id = book.Value!.Id, Title = "Other", Status = ReadingStatus.Unread });
        Assert.False(result.IsSuccess);
        Assert.Equal(0, _provider.Calls);
        Assert.NotNull((await mongo.FindBookByIdAsync(book.Value.Id))!.Embedding);
    }

    [Fact]
    public async Task IndexedBookReadsDoNotExposeIndexFields()
    {
        await SetPro(true);
        var book = await _books.TryAddBookAsync(Add());
        Assert.True(book.IsSuccess);
        using var client = _factory.CreateClient();
        foreach (var path in new[] { "/api/books", "/api/books/all", $"/api/books/{book.Value.Id}" })
        {
            var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("Synthetic", json);
            Assert.DoesNotContain("embedding", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task IndexedSearchResultDoesNotExposeIndexFields()
    {
        await SetPro(true);
        var book = await _books.TryAddBookAsync(Add());
        Assert.True(book.IsSuccess);
        using var client = _factory.CreateClient();
        var deadline = DateTime.UtcNow.AddSeconds(20);
        string json;
        do
        {
            using var response = await client.GetAsync("/api/books/search?searchTerm=Synthetic", TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            if (json.Contains(book.Value.Id, StringComparison.Ordinal)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Contains(book.Value.Id, json);
        Assert.DoesNotContain("embedding", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("score", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, _provider.Calls);
    }

    [Theory]
    [InlineData("wrong", false)]
    [InlineData("hang", false)]
    [InlineData("fail", false)]
    [InlineData("wrong", true)]
    [InlineData("hang", true)]
    [InlineData("fail", true)]
    public async Task UnusableProviderStillSaves(string mode, bool edit)
    {
        await SetPro(true);
        var initial = edit ? await _books.TryAddBookAsync(Add()) : null;
        _provider.Mode = mode;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        if (edit)
        {
            Assert.True((await _books.TryUpdateBookAsync(Edit(initial!.Value!.Id, "changed"))).IsSuccess);
            Assert.Null((await mongo.FindBookByIdAsync(initial.Value!.Id))!.Embedding);
        }
        else
        {
            var result = await _books.TryAddBookAsync(Add());
            Assert.True(result.IsSuccess);
            Assert.Null((await mongo.FindBookByIdAsync(result.Value.Id))!.Embedding);
        }
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(8));
    }

    private sealed class TestMetadata : IBookMetadataService
    {
        public Task<TryResult<BookMetadata>> TryGetBookMetadataAsync(string isbn) =>
            Task.FromResult<TryResult<BookMetadata>>(new BookMetadata { Title = "Synthetic ISBN", Isbn13 = isbn });
    }

    private sealed class FailedUserDao(string mode) : IUserDao
    {
        public Task<TryResult<User>> TryFindByIdAsync(string id) => mode == "throw"
            ? throw new InvalidOperationException("Synthetic lookup fault")
            : Task.FromResult<TryResult<User>>(new Error(mode == "missing" ? "Users.NotFound" : "Users.Failure", "Lookup failed"));
        public Task<TryResult<User>> TryFindByEmailAsync(string email) => throw new NotImplementedException();
        public Task<TryResult<bool>> TryUserExists(string email) => throw new NotImplementedException();
        public Task<TryResult<User>> TryAdd(NewUser user) => throw new NotImplementedException();
        public Task<TryResult<bool>> TryDeleteUserAsync(string userId) => throw new NotImplementedException();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);
        public void Dispose() { }
        private sealed class CapturingLogger(ConcurrentQueue<string> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => entries.Enqueue(formatter(state, exception) + exception);
        }
    }

    private sealed class TestProvider : IEmbeddingProvider
    {
        public int Calls { get; set; }
        public bool Fail { get; set; }
        public string ExceptionMessage { get; set; } = "synthetic sentinel";
        public string? Mode { get; set; }
        public string ModelId => "test-v1";
        public Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken)
        {
            Calls++;
            if (Mode == "hang") return Hang();
            if (Mode == "wrong") return Task.FromResult<float[]?>([1]);
            if (Fail || Mode == "fail") throw new InvalidOperationException(ExceptionMessage);
            var vector = new float[1536]; vector[0] = 1;
            return Task.FromResult<float[]?>(vector);
        }
        private static async Task<float[]?> Hang() { await Task.Delay(Timeout.InfiniteTimeSpan); return null; }
    }
}
