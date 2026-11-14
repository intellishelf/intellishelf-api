using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Explore;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.Models;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class ExploreFailureTests(MongoDbFixture mongo, AzuriteFixture azurite) : IAsyncLifetime
{
    private const string Sentinel = "synthetic-private-sentinel";
    private readonly Provider _provider = new();
    private readonly UserLookup _lookup = new("pro");
    private readonly Retrieval _retrieval = new();
    private readonly LogCapture _logs = new();
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public ValueTask InitializeAsync()
    {
        _factory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(builder => builder
            .ConfigureLogging(logging => logging.AddProvider(_logs))
            .ConfigureServices(services =>
            {
                services.RemoveAll<IUserDao>();
                services.AddSingleton<IUserDao>(_lookup);
                services.RemoveAll<IEmbeddingProvider>();
                services.AddSingleton<IEmbeddingProvider>(_provider);
                services.RemoveAll<IExploreDao>();
                services.AddSingleton<IExploreDao>(_retrieval);
                services.PostConfigure<ExploreOptions>(o => o.SearchTimeoutSeconds = 1);
            }));
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Explore-User-Id", DefaultTestUsers.Authenticated.Id);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }

    [Theory]
    [InlineData("throw")]
    [InlineData("wrong")]
    [InlineData("hang")]
    public async Task GivenUnusableProvider_WhenExploring_ThenReturnsGenericErrorWithoutRetrieval(string mode)
    {
        _provider.Mode = mode;
        var timer = Stopwatch.StartNew();
        using var response = await _client.PostAsJsonAsync("/api/books/explore", new { prompt = Sentinel }, TestContext.Current.CancellationToken);
        timer.Stop();
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5));
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Explore.Unavailable", body);
        Assert.DoesNotContain(Sentinel, body);
        Assert.Equal(1, _provider.Calls);
        Assert.Equal(0, _retrieval.Calls);
        Assert.DoesNotContain(_logs.Entries, x => x.Contains(Sentinel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GivenRetrievalFailure_WhenExploring_ThenReturnsGenericErrorWithoutPartialList()
    {
        _retrieval.Fail = true;
        using var response = await _client.PostAsJsonAsync("/api/books/explore", new { prompt = Sentinel }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Explore.Unavailable", body);
        Assert.DoesNotContain(Sentinel, body);
        Assert.DoesNotContain("items", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, _provider.Calls);
        Assert.Equal(1, _retrieval.Calls);
        Assert.DoesNotContain(_logs.Entries, x => x.Contains(Sentinel, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("error")]
    [InlineData("throw")]
    public async Task GivenFailedUserLookup_WhenExploring_ThenDeniesBeforeProviderAndRetrieval(string mode)
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserDao>();
            services.AddSingleton<IUserDao>(new UserLookup(mode));
        }));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Explore-User-Id", DefaultTestUsers.Authenticated.Id);
        using var response = await client.PostAsJsonAsync("/api/books/explore", new { prompt = Sentinel }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Explore.ProRequired", body);
        Assert.DoesNotContain(Sentinel, body);
        Assert.Equal(0, _provider.Calls);
        Assert.Equal(0, _retrieval.Calls);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("different")]
    [InlineData("duplicate")]
    public async Task GivenChangedSession_WhenExploring_ThenRejectsBeforeUserLookupOrProvider(string mode)
    {
        using var client = _factory.CreateClient();
        if (mode == "different") client.DefaultRequestHeaders.Add("X-Explore-User-Id", "different-synthetic-user");
        if (mode == "duplicate") client.DefaultRequestHeaders.TryAddWithoutValidation("X-Explore-User-Id", [DefaultTestUsers.Authenticated.Id, "different-synthetic-user"]);
        using var response = await client.PostAsJsonAsync("/api/books/explore", new { prompt = Sentinel }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Explore.SessionChanged", body);
        Assert.DoesNotContain(Sentinel, body);
        Assert.DoesNotContain("different-synthetic-user", body);
        Assert.Equal(0, _lookup.Calls);
        Assert.Equal(0, _provider.Calls);
        Assert.Equal(0, _retrieval.Calls);
        Assert.DoesNotContain(_logs.Entries, x => x.Contains(Sentinel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GivenAnonymousCallerWithoutSessionHeader_WhenExploring_ThenReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/books/explore");
        request.Headers.Add("X-Test-Anonymous", "1");
        request.Content = JsonContent.Create(new { prompt = Sentinel });
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _lookup.Calls);
        Assert.Equal(0, _provider.Calls);
        Assert.Equal(0, _retrieval.Calls);
    }

    [Fact]
    public async Task GivenMatchingSession_WhenExploring_ThenContinuesToLookupAndProvider()
    {
        using var response = await _client.PostAsJsonAsync("/api/books/explore", new { prompt = "Synthetic" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, _lookup.Calls);
        Assert.Equal(1, _provider.Calls);
        Assert.Equal(1, _retrieval.Calls);
    }

    private sealed class Provider : IEmbeddingProvider
    {
        public string ModelId => "test";
        public int Calls { get; private set; }
        public string Mode { get; set; } = "ok";
        public Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken)
        {
            Calls++;
            if (Mode == "throw") throw new InvalidOperationException(Sentinel);
            if (Mode == "wrong") return Task.FromResult<float[]?>([1]);
            if (Mode == "hang") return new TaskCompletionSource<float[]?>().Task;
            var result = new float[1536]; result[0] = 1;
            return Task.FromResult<float[]?>(result);
        }
    }
    private sealed class Retrieval : IExploreDao
    {
        public bool Fail { get; set; }
        public int Calls { get; private set; }
        public Task<TryResult<ExploreMatches>> TrySearchAsync(string userId, string[] words, float[] vector, int bound, CancellationToken cancellationToken)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException(Sentinel);
            return Task.FromResult<TryResult<ExploreMatches>>(new ExploreMatches([], [], []));
        }
        public Task<TryResult<IReadOnlyList<ExploreBook>>> TryLoadAsync(string userId, IEnumerable<string> ids, CancellationToken cancellationToken) => Task.FromResult<TryResult<IReadOnlyList<ExploreBook>>>(new List<ExploreBook>());
    }
    private sealed class UserLookup(string mode) : IUserDao
    {
        public int Calls { get; private set; }
        public Task<TryResult<User>> TryFindByIdAsync(string id)
        {
            Calls++;
            return mode switch
            {
                "throw" => throw new InvalidOperationException(Sentinel),
                "missing" => Task.FromResult<TryResult<User>>(new Error("Users.NotFound", "Missing")),
                "error" => Task.FromResult<TryResult<User>>(new Error("Users.Failure", "Failed")),
                _ => Task.FromResult<TryResult<User>>(new User(id, "synthetic@example.test", null, null, default, null, true))
            };
        }
        public Task<TryResult<User>> TryFindByEmailAsync(string email) => throw new NotImplementedException();
        public Task<TryResult<bool>> TryUserExists(string email) => throw new NotImplementedException();
        public Task<TryResult<User>> TryAdd(NewUser user) => throw new NotImplementedException();
        public Task<TryResult<bool>> TryDeleteUserAsync(string userId) => throw new NotImplementedException();
    }
    private sealed class LogCapture : ILoggerProvider
    {
        public ConcurrentQueue<string> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new Capture(Entries);
        public void Dispose() { }
        private sealed class Capture(ConcurrentQueue<string> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => entries.Enqueue(formatter(state, exception) + exception);
        }
    }
}
