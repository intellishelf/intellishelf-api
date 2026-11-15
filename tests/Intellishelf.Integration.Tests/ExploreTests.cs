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
        _client.DefaultRequestHeaders.Add("X-Explore-User-Id", UserId);
    }
    public async ValueTask DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }
    private Task Pro(bool value) => mongo.Database.GetCollection<UserEntity>(UserEntity.CollectionName).UpdateOneAsync(u => u.Id == UserId, Builders<UserEntity>.Update.Set(u => u.IsPro, value));
    private Task<HttpResponseMessage> Query(string prompt) => _client.PostAsJsonAsync("/api/books/explore", new { prompt }, TestContext.Current.CancellationToken);
    private async Task<BookEntity> Book(string title, string author, string description, string owner, string embeddingText)
    {
        return new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = ObjectId.Parse(owner), Title = title, Authors = [author], Description = description, Embedding = await _provider.GenerateAsync(embeddingText, CancellationToken.None), CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
    }
    [Fact]
    public async Task GivenAuthenticationAndOtherTenants_WhenExploring_ThenValidatesAndIsolates()
    {
        using var anon = new HttpRequestMessage(HttpMethod.Post, "/api/books/explore");
        anon.Headers.Add("X-Test-Anonymous", "1");
        anon.Content = JsonContent.Create(new { prompt = "Dune" });
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(anon, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(0, _provider.Calls);
        Assert.Equal(HttpStatusCode.Forbidden, (await Query("Dune")).StatusCode);
        await Pro(true);
        var invalid = await Query(new string('x', 501));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.DoesNotContain(new string('x', 501), await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var foreign = ObjectId.GenerateNewId().ToString();
        var owned = await Book("Dune", "Frank Herbert", "ecology", UserId, "Dune");
        var strangers = new[] { await Book("Dune", "Other", "", foreign, "other"), await Book("Other", "Frank Herbert", "", foreign, "other"), await Book("Other", "Other", "ecology", foreign, "other"), await Book("Other", "Other", "", foreign, "Dune") };
        await mongo.SeedBooksAsync([owned, .. strangers]);
        await mongo.Database.GetCollection<UserEntity>(UserEntity.CollectionName).InsertOneAsync(new UserEntity { Id = foreign, Email = "other@example.test", AuthProvider = Intellishelf.Domain.Users.Models.AuthProvider.Email, IsPro = true }, cancellationToken: TestContext.Current.CancellationToken);
        using var foreignClient = _factory.CreateClient();
        foreignClient.DefaultRequestHeaders.Add("X-Test-User", foreign);
        foreignClient.DefaultRequestHeaders.Add("X-Explore-User-Id", foreign);
        foreach (var (query, target) in new[] { ("Dune", strangers[0].Id), ("Herbrt", strangers[1].Id), ("ecology", strangers[2].Id), ("Dune", strangers[3].Id) })
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            bool found;
            do
            {
                using var response = await foreignClient.PostAsJsonAsync("/api/books/explore", new { prompt = query }, TestContext.Current.CancellationToken);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken);
                found = result!.Items.Any(b => b.Id == target);
                if (!found) await Task.Delay(250, TestContext.Current.CancellationToken);
            } while (!found && DateTime.UtcNow < deadline);
            Assert.True(found, $"Foreign tenant did not find {target} for {query}.");
        }
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
    public async Task GivenLargeMatches_WhenExploring_ThenReturnsAllAndReportsBound()
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
        boundedClient.DefaultRequestHeaders.Add("X-Explore-User-Id", UserId);
        using var boundedResponse = await boundedClient.PostAsJsonAsync("/api/books/explore", new { prompt = "atlas" }, TestContext.Current.CancellationToken);
        boundedResponse.EnsureSuccessStatusCode();
        var bounded = await boundedResponse.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken);
        Assert.Equal(50, bounded!.Items.Count);
        Assert.True(bounded.Truncated);
        Assert.Equal(all.Items.Take(50).Select(b => b.Id), bounded.Items.Select(b => b.Id));
    }

    [Fact]
    public async Task GivenInvalidRequests_WhenExploring_ThenRejectsWithoutLeakingOrCallingProvider()
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
    [Fact]
    public async Task GivenWhitespaceAndCompetingMatches_WhenExploring_ThenExactTitlesAndAuthorsRankFirst()
    {
        await Pro(true);
        var books = new[]
        {
            await Book(" Dune ", "Writer", "", UserId, "other"),
            await Book("Dune Messiah", "Writer", "", UserId, "Dune"),
            await Book("Other", " Frank Herbert ", "", UserId, "other"),
            await Book("Other Two", "Frank Herbert Jr", "", UserId, "Frank Herbert")
        };
        await mongo.SeedBooksAsync(books);
        async Task<ExploreResult> Wait(string prompt, string exactId)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            ExploreResult result;
            do
            {
                using var response = await Query(prompt);
                response.EnsureSuccessStatusCode();
                result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
                if (result.Items.Any(x => x.Id == exactId) && result.Items.Count >= 2) break;
                await Task.Delay(250, TestContext.Current.CancellationToken);
            } while (DateTime.UtcNow < deadline);
            Assert.Equal(exactId, result.Items.First().Id);
            Assert.True(result.Items.Count >= 2);
            return result;
        }
        await Wait("Dune", books[0].Id);
        await Wait("Frank Herbert", books[2].Id);
    }

    [Fact]
    public async Task GivenLongUnindexedTitle_WhenExploring_ThenAllWordsAreRequiredWithinDeadline()
    {
        await Pro(true);
        var words = Enumerable.Range(0, 100).Select(x => $"w{x:000}").ToArray();
        var prompt = string.Join(' ', words) + "x";
        Assert.Equal(500, prompt.Length);
        words[^1] += "x";
        var full = await Book(prompt, "Writer", "", UserId, "unrelated");
        var partial = await Book(string.Join(' ', words.Take(99)), "Writer", "", UserId, "unrelated");
        // Deliberately no vectors: only all-word retrieval can find the title.
        full = new BookEntity { Id = full.Id, UserId = full.UserId, Title = full.Title, Authors = full.Authors, CreatedDate = full.CreatedDate, ModifiedDate = full.ModifiedDate, Status = full.Status };
        partial = new BookEntity { Id = partial.Id, UserId = partial.UserId, Title = partial.Title, Authors = partial.Authors, CreatedDate = partial.CreatedDate, ModifiedDate = partial.ModifiedDate, Status = partial.Status };
        await mongo.SeedBooksAsync(full, partial);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult result;
        do
        {
            using var response = await Query(prompt);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            if (result.Items.Any(b => b.Id == full.Id)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Equal(full.Id, Assert.Single(result.Items).Id);
        var distinct = Enumerable.Range(0, 33).Select(i => $"p{new string((char)('a' + i / 26), 3)}{new string((char)('a' + i % 26), 3)}").ToArray();
        var prompt33 = string.Join(' ', distinct);
        var full33 = new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = ObjectId.Parse(UserId), Title = prompt33, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
        var partial33 = new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = ObjectId.Parse(UserId), Title = string.Join(' ', distinct.Take(32)), CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
        await mongo.SeedBooksAsync(full33, partial33);
        deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult shortResult;
        do
        {
            using var shortResponse = await Query(prompt33);
            shortResponse.EnsureSuccessStatusCode();
            shortResult = (await shortResponse.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            if (shortResult.Items.Any(b => b.Id == full33.Id)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Contains(shortResult.Items, b => b.Id == full33.Id);
        Assert.DoesNotContain(shortResult.Items, b => b.Id == partial33.Id);
    }

    [Fact]
    public async Task GivenFirstCharacterTypo_WhenExploring_ThenFuzzyTitleRanksAboveSemanticOnly()
    {
        await Pro(true);
        var keyword = await Book("Dune", "Writer", "", UserId, "unrelated");
        var meaning = await Book("Completely Other", "Writer", "", UserId, "Cune");
        await mongo.SeedBooksAsync(keyword, meaning);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult result;
        do
        {
            using var response = await Query("Cune"); response.EnsureSuccessStatusCode();
            result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            if (result.Items.Count >= 2) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Equal(new[] { keyword.Id, meaning.Id }, result.Items.Take(2).Select(b => b.Id));
    }

    [Fact]
    public async Task GivenBoundedVectorHits_WhenExploring_ThenOnlyOmittedRelevantHitsMarkTruncated()
    {
        await Pro(true);
        using var boundedFactory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(b => b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?> { ["Explore:SafetyBound"] = "50" })).ConfigureServices(s => { s.RemoveAll<IEmbeddingProvider>(); s.AddSingleton<IEmbeddingProvider>(_provider); }));
        using var client = boundedFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Explore-User-Id", UserId);
        var high = await _provider.GenerateAsync("vectorquery", CancellationToken.None);
        var low = new float[1536]; low[(Array.FindIndex(high!, x => x != 0) + 1) % 1536] = 1;
        var all = Enumerable.Range(0, 51).Select(i => new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = ObjectId.Parse(UserId), Title = $"Synthetic {i}", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread, Embedding = i < 2 ? high : low }).ToArray();
        await mongo.SeedBooksAsync(all);
        async Task Wait(int expected, bool truncated)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            ExploreResult result;
            do
            {
                using var response = await client.PostAsJsonAsync("/api/books/explore", new { prompt = "vectorquery" }, TestContext.Current.CancellationToken);
                response.EnsureSuccessStatusCode();
                result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
                if (result.Items.Count == expected && result.Truncated == truncated) return;
                await Task.Delay(250, TestContext.Current.CancellationToken);
            } while (DateTime.UtcNow < deadline);
            Assert.Equal(expected, result.Items.Count); Assert.Equal(truncated, result.Truncated);
        }
        await Wait(2, false);
        var collection = mongo.Database.GetCollection<BookEntity>(BookEntity.CollectionName);
        await collection.UpdateManyAsync(Builders<BookEntity>.Filter.In(x => x.Id, all.Skip(2).Take(48).Select(x => x.Id)), Builders<BookEntity>.Update.Set(x => x.Embedding, high), cancellationToken: TestContext.Current.CancellationToken);
        await Wait(50, false);
        await collection.UpdateOneAsync(x => x.Id == all[50].Id, Builders<BookEntity>.Update.Set(x => x.Embedding, high), cancellationToken: TestContext.Current.CancellationToken);
        await Wait(50, true);
    }

    [Fact]
    public async Task GivenIndexedBook_WhenDeleted_ThenExploreStopsReturningIt()
    {
        await Pro(true);
        var book = await Book("Synthetic delete", "Writer", "", UserId, "vectorquery");
        await mongo.SeedBooksAsync(book);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult result;
        do
        {
            using var response = await Query("vectorquery"); response.EnsureSuccessStatusCode();
            result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            if (result.Items.Any(x => x.Id == book.Id)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Contains(result.Items, x => x.Id == book.Id);
        using var deletion = await _client.DeleteAsync($"/api/books/{book.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
        using var after = await Query("vectorquery"); after.EnsureSuccessStatusCode();
        Assert.DoesNotContain((await after.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!.Items, x => x.Id == book.Id);
    }

    [Fact]
    public async Task GivenExactCrossFieldAndTiedVectorHits_WhenExploring_ThenTiersAndTiesRemainStable()
    {
        await Pro(true);
        var exact = await Book("Dune Herbert", "Writer", "", UserId, "unrelated");
        var cross = await Book("Dune", "Herbert", "", UserId, "unrelated");
        var semanticA = await Book("Quiet A", "Writer", "", UserId, "Dune Herbert");
        var semanticB = await Book("Quiet B", "Writer", "", UserId, "Dune Herbert");
        Assert.Equal(semanticA.Embedding, semanticB.Embedding);
        await mongo.SeedBooksAsync(exact, cross, semanticA, semanticB);
        async Task<ExploreResult> Search()
        {
            using var response = await Query("Dune Herbert"); response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
        }
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult first;
        do
        {
            first = await Search();
            if (first.Items.Count == 4) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Equal(4, first.Items.Count);
        Assert.Equal(exact.Id, first.Items[0].Id);
        Assert.Equal(cross.Id, first.Items[1].Id);
        Assert.Equal(new[] { semanticA.Id, semanticB.Id }.Order(), first.Items.Skip(2).Select(x => x.Id));
        Assert.Equal(first.Items.Select(x => x.Id), (await Search()).Items.Select(x => x.Id));
    }

    [Fact]
    public async Task GivenLighthouseBook_WhenSearchingByLoneliness_ThenStandInMatchesAndRejectsUnrelatedPrompt()
    {
        await Pro(true);
        var request = new AddBookRequest { UserId = UserId, Title = "The Quiet Lighthouse", Description = "A keeper lives alone in solitude.", Status = ReadingStatus.Unread };
        var text = IndexedBookText.Build(request, 12000);
        var book = await Book(request.Title, "", request.Description, UserId, text);
        await mongo.SeedBooksAsync(book);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult result;
        do
        {
            using var response = await Query("Stories of loneliness and isolation"); response.EnsureSuccessStatusCode();
            result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            if (result.Items.Any(x => x.Id == book.Id)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Contains(result.Items, x => x.Id == book.Id);
        using var unrelated = await Query("books about quantum physics"); unrelated.EnsureSuccessStatusCode();
        Assert.Empty((await unrelated.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!.Items);
    }

    [Fact]
    public async Task GivenBatchDistractors_WhenExploring_ThenCompleteHitIsNotDiscardedBeforeIntersection()
    {
        await Pro(true);
        using var factory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(b => b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?> { ["Explore:SafetyBound"] = "5" })).ConfigureServices(s => { s.RemoveAll<IEmbeddingProvider>(); s.AddSingleton<IEmbeddingProvider>(_provider); }));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Explore-User-Id", UserId);
        const string prefix = "orchid maple quartz lantern";
        const string prompt = prefix + " zebra velvet canyon harbor";
        var full = new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = ObjectId.Parse(UserId), Title = prompt, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
        var distractors = Enumerable.Range(0, 20).Select(i => new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = full.UserId, Title = $"{prefix} {prefix} {prefix} {i}", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread }).ToArray();
        await mongo.SeedBooksAsync([full, .. distractors]);
        async Task<ExploreResult> Search(string query)
        {
            using var response = await client.PostAsJsonAsync("/api/books/explore", new { prompt = query }, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
        }
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult complete;
        do { complete = await Search(prompt); if (complete.Items.Any(x => x.Id == full.Id)) break; await Task.Delay(250, TestContext.Current.CancellationToken); } while (DateTime.UtcNow < deadline);
        Assert.Equal(full.Id, Assert.Single(complete.Items).Id);
        var prefixOnly = await Search(prefix);
        Assert.Equal(5, prefixOnly.Items.Count);
        Assert.DoesNotContain(prefixOnly.Items, x => x.Id == full.Id);
        Assert.True(prefixOnly.Truncated);
    }

    [Fact]
    public async Task GivenPunctuationDensePrompt_WhenExploring_ThenRespondsWithinDeadline()
    {
        await Pro(true);
        var prompt = string.Join(',', Enumerable.Range(0, 100).Select(x => $"w{x:000}")) + "x";
        Assert.Equal(500, prompt.Length);
        var full = new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = ObjectId.Parse(UserId), Title = prompt, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
        var partial = new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = full.UserId, Title = string.Join(',', Enumerable.Range(0, 99).Select(x => $"w{x:000}")), CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
        await mongo.SeedBooksAsync(full, partial);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        do
        {
            using var readiness = await Query("w000");
            readiness.EnsureSuccessStatusCode();
            var ready = await readiness.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken);
            if (ready!.Items.Any(x => x.Id == full.Id)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        ExploreResult result;
        do
        {
            var timer = Stopwatch.StartNew();
            using var response = await Query(prompt);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10));
            result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            if (result.Items.Any(x => x.Id == full.Id)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Contains(result.Items, x => x.Id == full.Id);
        Assert.DoesNotContain(result.Items, x => x.Id == partial.Id);
    }

    [Theory]
    [InlineData(',')]
    [InlineData('-')]
    public async Task GivenOnlyDelimiters_WhenExploring_ThenReturnsSemanticOnlyResults(char delimiter)
    {
        await Pro(true);
        var prompt = new string(delimiter, 33);
        using var initial = await Query(prompt);
        initial.EnsureSuccessStatusCode();
        Assert.Empty((await initial.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!.Items);

        var book = new BookEntity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = ObjectId.Parse(UserId),
            Title = "Synthetic vector-only match",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Status = ReadingStatus.Unread,
            Embedding = await _provider.GenerateAsync(prompt, TestContext.Current.CancellationToken)
        };
        await mongo.SeedBooksAsync(book);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult result;
        do
        {
            using var response = await Query(prompt);
            response.EnsureSuccessStatusCode();
            result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            if (result.Items.Any(item => item.Id == book.Id)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Equal(book.Id, Assert.Single(result.Items).Id);
    }

    [Theory]
    [InlineData("Lion's")]
    [InlineData("Lion’s")]
    [InlineData("1,234")]
    public async Task GivenOrdinaryPunctuation_WhenExploring_ThenPreservesExactMatch(string title)
    {
        await Pro(true);
        var book = new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = ObjectId.Parse(UserId), Title = title, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
        await mongo.SeedBooksAsync(book);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult result;
        do
        {
            using var response = await Query(title); response.EnsureSuccessStatusCode();
            result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            if (result.Items.Any(x => x.Id == book.Id)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Equal(book.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GivenMultipartBookWithEmptyAuthor_WhenExploring_ThenReturnsMatchingBook()
    {
        await Pro(true);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Synthetic Lighthouse"), "Title");
        form.Add(new StringContent(""), "Authors");
        form.Add(new StringContent("Unread"), "Status");
        using var save = await _client.PostAsync("/api/books", form, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, save.StatusCode);
        var book = await save.Content.ReadFromJsonAsync<Book>(TestContext.Current.CancellationToken);
        Assert.NotNull(book);
        var stored = await mongo.FindBookByIdAsync(book.Id);
        Assert.NotNull(stored?.Authors);
        Assert.Contains(stored.Authors, author => author is null);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ExploreResult result;
        do
        {
            using var response = await Query("Lighthouse");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            if (result.Items.Any(x => x.Id == book.Id)) break;
            await Task.Delay(250, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);
        Assert.Contains(result.Items, x => x.Id == book.Id);
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
