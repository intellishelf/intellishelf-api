using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
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
public sealed class ThematicTagsReplayTests(MongoDbFixture mongo, AzuriteFixture azurite)
{
    private static readonly string Dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private sealed record Book(string Id, string Title, string[] Authors, string? Description);
    private sealed record Query(string Id, string Prompt, string Split);
    private sealed record Corpus(Book[] Books, Query[] Queries);
    private sealed record Spec(Dictionary<string, string[]> Tags);
    private sealed record VectorFile(string Model, int Dimensions, string InputTemplateVersion, Dictionary<string, string> Vectors);
    private static string PathFor(string name) => Path.Combine(Dir, name);
    private static byte[] Frozen(string file, string hash) { var bytes = File.ReadAllBytes(PathFor(file)); Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(bytes))); return bytes; }
    [Fact]
    public async Task ReplayCalibrationVariantOnlyWhenRequested()
    {
        var destination = Environment.GetEnvironmentVariable("THEMATIC_TAG_OUTPUT"); if (destination is null) return;
        var tagged = Environment.GetEnvironmentVariable("THEMATIC_TAG_MODE") == "on";
        var cutoff = double.Parse(Environment.GetEnvironmentVariable("THEMATIC_CUTOFF") ?? "0.7285", System.Globalization.CultureInfo.InvariantCulture);
        var corpus = JsonSerializer.Deserialize<Corpus>(Frozen("thematic-corpus.json", "0D36FAE51AC228F2E7A1B4913086D56D4B25FA8614F8FB919391FDAF9C128CC3"), Json)!;
        var spec = JsonSerializer.Deserialize<Spec>(Frozen("thematic-tag-spec.json", "4FA6D4CBBF0FB2FA671A9F915609C06785D93BF54958F471D7C5A7496C9B5BBD"), Json)!;
        var original = JsonSerializer.Deserialize<RecordedVectorsTests.Recording>(File.ReadAllText(PathFor("thematic-vectors.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var variants = JsonSerializer.Deserialize<VectorFile>(File.ReadAllText(PathFor("thematic-tag-vectors.json")), Json)!;
        Assert.Equal("text-embedding-3-large", original.Model); Assert.Equal(original.Model, variants.Model); Assert.Equal(1536, variants.Dimensions); Assert.Equal(IndexedBookText.Version, variants.InputTemplateVersion);
        var vectors = original.Vectors.Concat(variants.Vectors).GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.First().Value);
        float[] Find(string text)
        {
            Assert.True(vectors.TryGetValue(RecordedVectorsTests.Hash(text), out var value), "Missing recorded vector.");
            var bytes = Convert.FromBase64String(value!); var result = new float[bytes.Length / sizeof(float)]; Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length); Assert.Equal(1536, result.Length); return result;
        }
        var provider = new ReplayProvider(Find);
        await mongo.ClearBooksAsync(); await mongo.SeedDefaultUserAsync();
        await mongo.Database.GetCollection<UserEntity>(UserEntity.CollectionName).UpdateOneAsync(u => u.Id == DefaultTestUsers.Authenticated.Id, Builders<UserEntity>.Update.Set(u => u.IsPro, true), cancellationToken: TestContext.Current.CancellationToken);
        var ids = corpus.Books.ToDictionary(b => b.Id, _ => ObjectId.GenerateNewId().ToString());
        var books = corpus.Books.Select(b =>
        {
            var description = b.Description;
            var tags = tagged ? spec.Tags[b.Id] : null;
            var text = IndexedBookText.Build(new AddBookRequest { UserId = "synthetic", Title = b.Title, Authors = b.Authors, Description = description, Tags = tags, Status = ReadingStatus.Unread }, 12000);
            return new BookEntity { Id = ids[b.Id], UserId = ObjectId.Parse(DefaultTestUsers.Authenticated.Id), Title = b.Title, Authors = b.Authors, Description = description, Tags = tags, Embedding = Find(text), CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread };
        }).ToArray();
        await mongo.SeedBooksAndWaitForIndexing(books);
        await using var factory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IEmbeddingProvider>(); s.AddSingleton<IEmbeddingProvider>(provider); s.PostConfigure<ExploreOptions>(o => o.RelevanceCutoff = cutoff); }));
        using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Explore-User-Id", DefaultTestUsers.Authenticated.Id);
        var results = new Dictionary<string, string[]>();
        foreach (var q in corpus.Queries.Where(q => q.Split == "calibration"))
        {
            using var response = await client.PostAsJsonAsync("/api/books/explore", new { prompt = q.Prompt }, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode(); var result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!; Assert.False(result.Truncated);
            results[q.Id] = result.Items.Select(b => ids.Single(pair => pair.Value == b.Id).Key).ToArray();
        }
        await File.WriteAllTextAsync(destination, JsonSerializer.Serialize(results, Json), TestContext.Current.CancellationToken);
    }
    private sealed class ReplayProvider(Func<string, float[]> find) : IEmbeddingProvider
    {
        public string ModelId => "text-embedding-3-large";
        public Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken) => Task.FromResult<float[]?>(find(text));
    }
}
