using System.Net;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Explore;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class ExploreTests(MongoDbFixture mongo, AzuriteFixture azurite) : IAsyncLifetime
{
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private readonly SpyProvider _provider = new();
    private readonly CaptureLogs _logs = new();
    private string UserId => DefaultTestUsers.Authenticated.Id;
    public async ValueTask InitializeAsync()
    {
        await mongo.ClearBooksAsync();
        await mongo.SeedDefaultUserAsync();
        _factory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(_logs)).ConfigureServices(s =>
        {
            s.RemoveAll<IEmbeddingProvider>();
            s.AddSingleton<IEmbeddingProvider>(_provider);
        }));
        _client = _factory.CreateClient();
    }
    public async ValueTask DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }
    private Task Pro(bool value) => mongo.Database.GetCollection<UserEntity>(UserEntity.CollectionName).UpdateOneAsync(u => u.Id == UserId, Builders<UserEntity>.Update.Set(u => u.IsPro, value));
    private Task<HttpResponseMessage> Query(string prompt) => _client.PostAsJsonAsync("/api/books/explore", new { prompt }, TestContext.Current.CancellationToken);
    private async Task<BookEntity> Book(string title, string author, string description, string owner, string embeddingText)
    {
        return new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = ObjectId.Parse(owner), Title = title, Authors = [author], Description = description, Embedding = await _provider.GenerateAsync(embeddingText, CancellationToken.None), CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
    }
    [Fact]
    public async Task AuthValidationAndUserIsolation()
    {
        using var anon = new HttpRequestMessage(HttpMethod.Post, "/api/books/explore");
        anon.Headers.Add("X-Test-Anonymous", "1");
        anon.Content = JsonContent.Create(new { prompt = "Dune" });
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(anon, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Query("Dune")).StatusCode);
        await Pro(true);
        var invalid = await Query(new string('x', 501));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.DoesNotContain(new string('x', 501), await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var foreign = ObjectId.GenerateNewId().ToString();
        var owned = await Book("Dune", "Frank Herbert", "ecology", UserId, "Dune");
        var strangers = new[] { await Book("Dune", "Other", "", foreign, "other"), await Book("Other", "Frank Herbert", "", foreign, "other"), await Book("Other", "Other", "ecology", foreign, "other"), await Book("Other", "Other", "", foreign, "Dune") };
        await mongo.SeedBooksAndWaitForIndexing([owned, .. strangers]);
        foreach (var prompt in new[] { "Dune", "Herbert", "Herbrt", "ecology" })
        {
            var response = await Query(prompt);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains(owned.Id, json);
            foreach (var stranger in strangers) Assert.DoesNotContain(stranger.Id, json);
            Assert.DoesNotContain("embedding", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("score", json, StringComparison.OrdinalIgnoreCase);
        }
        var first = await (await Query("Dune Herbert")).Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken);
        var second = await (await Query("Dune Herbert")).Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken);
        Assert.NotNull(first); Assert.NotNull(second);
        Assert.Contains(first.Items, b => b.Id == owned.Id);
        Assert.Equal(first.Items.Select(b => b.Id), second.Items.Select(b => b.Id));
        var cross = await (await Query("Dune ecology")).Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken);
        Assert.Contains(cross!.Items, b => b.Id == owned.Id);
        var longPrompt = string.Join(' ', Enumerable.Range(0, 100).Select(x => $"w{x:000}")) + "x";
        Assert.Equal(500, longPrompt.Length);
        Assert.Equal(HttpStatusCode.OK, (await Query(longPrompt)).StatusCode);
    }
    [Fact]
    public async Task LargeResultReturnsAllAndReportsBound()
    {
        await Pro(true);
        var books = Enumerable.Range(0, 300).Select(i => new BookEntity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = ObjectId.Parse(UserId),
            Title = $"Synthetic atlas {i}",
            Description = new string('z', 1000),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Status = ReadingStatus.Unread
        }).ToArray();
        await mongo.SeedBooksAsync(books);
        var until = DateTime.UtcNow.AddSeconds(30);
        string json;
        var timer = new Stopwatch();
        do
        {
            timer.Restart();
            using var response = await Query("atlas");
            response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            timer.Stop();
            var result = System.Text.Json.JsonSerializer.Deserialize<ExploreResult>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result?.Items.Count == 300) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < until);
        var all = System.Text.Json.JsonSerializer.Deserialize<ExploreResult>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(300, all!.Items.Count);
        Assert.False(all.Truncated);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(json) < 1_000_000);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10));
        var metricsPath = Environment.GetEnvironmentVariable("EXPLORE_TEST_METRICS_PATH");
        if (metricsPath is not null) await File.WriteAllTextAsync(metricsPath, $"bytes={System.Text.Encoding.UTF8.GetByteCount(json)}; elapsed_ms={timer.ElapsedMilliseconds}", TestContext.Current.CancellationToken);
        using var boundedFactory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(b => b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?> { ["Explore:SafetyBound"] = "50" })).ConfigureServices(s =>
        {
            s.RemoveAll<IEmbeddingProvider>(); s.AddSingleton<IEmbeddingProvider>(_provider);
        }));
        using var boundedClient = boundedFactory.CreateClient();
        using var boundedResponse = await boundedClient.PostAsJsonAsync("/api/books/explore", new { prompt = "atlas" }, TestContext.Current.CancellationToken);
        boundedResponse.EnsureSuccessStatusCode();
        var bounded = await boundedResponse.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken);
        Assert.Equal(50, bounded!.Items.Count);
        Assert.True(bounded.Truncated);
        Assert.Equal(all.Items.Take(50).Select(b => b.Id), bounded.Items.Select(b => b.Id));
    }

    [Fact]
    public async Task ProviderFailuresAndInvalidRequestsDoNotLeakOrCallProviderEarly()
    {
        const string sentinel = "synthetic-private-sentinel";
        var nonPro = await Query(sentinel);
        Assert.Equal(HttpStatusCode.Forbidden, nonPro.StatusCode);
        Assert.Equal(0, _provider.Calls);
        await Pro(true);
        var invalid = await Query(new string('x', 501) + sentinel);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.DoesNotContain(sentinel, await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, _provider.Calls);
        _provider.Fail = true;
        var failure = await Query(sentinel);
        Assert.Equal(HttpStatusCode.BadGateway, failure.StatusCode);
        Assert.DoesNotContain(sentinel, await failure.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.DoesNotContain(_logs.Entries, entry => entry.Contains(sentinel, StringComparison.Ordinal));
    }
    private sealed class SpyProvider : IEmbeddingProvider
    {
        private readonly StandInEmbeddingProvider _inner = new(Options.Create(new ExploreOptions()));
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public string ModelId => _inner.ModelId;
        public Task<float[]?> GenerateAsync(string text, CancellationToken token)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException(text);
            return _inner.GenerateAsync(text, token);
        }
    }
    private sealed class CaptureLogs : ILoggerProvider
    {
        public ConcurrentQueue<string> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new Capture(Entries);
        public void Dispose() { }
        private sealed class Capture(ConcurrentQueue<string> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel level) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter) => entries.Enqueue(formatter(state, error) + error);
        }
    }

}
