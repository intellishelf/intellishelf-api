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
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class RecordedExploreTests(MongoDbFixture mongo, AzuriteFixture azurite) : IAsyncLifetime
{
    private readonly ReplayProvider _provider = new();
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    public async ValueTask InitializeAsync()
    {
        await mongo.ClearBooksAsync(); await mongo.SeedDefaultUserAsync();
        await mongo.Database.GetCollection<UserEntity>(UserEntity.CollectionName).UpdateOneAsync(u => u.Id == DefaultTestUsers.Authenticated.Id, Builders<UserEntity>.Update.Set(u => u.IsPro, true));
        _factory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IEmbeddingProvider>(); s.AddSingleton<IEmbeddingProvider>(_provider); }));
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Explore-User-Id", DefaultTestUsers.Authenticated.Id);
    }
    public async ValueTask DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }
    [Fact]
    public async Task GivenRecordedVectors_WhenExploring_ThenRanksAndAppliesCutoff()
    {
        var corpus = RecordedVectorsTests.ReadCorpus();
        var books = corpus.Books.Select(b =>
        {
            var request = new AddBookRequest { UserId = DefaultTestUsers.Authenticated.Id, Title = b.Title, Authors = b.Authors, Description = b.Description, Status = ReadingStatus.Unread };
            var text = IndexedBookText.Build(request, 12000);
            return new BookEntity { Id = ObjectId.GenerateNewId().ToString(), UserId = ObjectId.Parse(request.UserId), Title = b.Title, Authors = b.Authors, Description = b.Description, Embedding = _provider.Find(text), CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
        }).ToArray();
        await mongo.SeedBooksAndWaitForIndexing(books);
        async Task<ExploreResult> Search(string prompt)
        {
            using var response = await _client.PostAsJsonAsync("/api/books/explore", new { prompt }, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
        }
        var until = DateTime.UtcNow.AddSeconds(25);
        ExploreResult dune;
        do { dune = await Search("Dune"); if (dune.Items.Any(b => b.Title == "Dune")) break; await Task.Delay(250, TestContext.Current.CancellationToken); } while (DateTime.UtcNow < until);
        Assert.Equal("Dune", dune.Items.First().Title);
        ExploreResult author;
        do { author = await Search("Shakespeare"); if (author.Items.Count(b => b.Authors!.Contains("Shakespeare")) >= 2) break; await Task.Delay(250, TestContext.Current.CancellationToken); } while (DateTime.UtcNow < until);
        Assert.True(author.Items.Count >= 2);
        Assert.Equal(new[] { "Hamlet", "King Lear" }.Order(), author.Items.Take(2).Select(b => b.Title).Order());
        var fuzzy = await Search("Shakespare");
        Assert.Contains(fuzzy.Items.Take(2), b => b.Title == "Hamlet");
        Assert.Contains(fuzzy.Items.Take(2), b => b.Title == "King Lear");
        var semantic = await Search("books about loneliness and isolation");
        Assert.Contains(semantic.Items, b => b.Title == "The Lonely Room");
        Assert.Contains(semantic.Items, b => b.Title == "Island Silence");
        Assert.Empty((await Search("books about quantum physics")).Items);
        Assert.Equal(dune.Items.Select(b => b.Id), (await Search("Dune")).Items.Select(b => b.Id));
    }
    private sealed class ReplayProvider : IEmbeddingProvider
    {
        private readonly Dictionary<string, float[]> _vectors = RecordedVectorsTests.ReadVectors();
        public string ModelId => "text-embedding-3-large";
        public float[] Find(string text) => _vectors.TryGetValue(RecordedVectorsTests.Hash(text), out var vector) ? vector : throw new InvalidOperationException("Re-record the fixtures.");
        public Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken) => Task.FromResult<float[]?>(Find(text));
    }
}
