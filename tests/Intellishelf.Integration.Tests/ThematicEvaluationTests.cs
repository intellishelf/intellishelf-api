using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http.Json;
using System.Text.Json;
using Intellishelf.Api.Controllers;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Explore;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenAI;
using OpenAI.Embeddings;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class ThematicEvaluationTests(MongoDbFixture mongo, AzuriteFixture azurite)
{
    private static readonly string Dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static string PathFor(string name) => Path.Combine(Dir, name);
    private sealed record Book(string Id, string Title, string[] Authors, string? Description, string Metadata);
    private sealed record Query(string Id, string Prompt, string IntentGroup, string Split, string Category, string Language, string[] Relevant, string[] Borderline, string[] Irrelevant, bool RankRelevantBeforeBorderline);
    private sealed record Corpus(int Version, Book[] Books, Query[] Queries);
    private static Corpus ReadCorpus()
    {
        var bytes = File.ReadAllBytes(PathFor("thematic-corpus.json"));
        Assert.Equal("0D36FAE51AC228F2E7A1B4913086D56D4B25FA8614F8FB919391FDAF9C128CC3", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
        return JsonSerializer.Deserialize<Corpus>(bytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })!;
    }
    private static string BookText(Book b) => IndexedBookText.Build(new AddBookRequest { UserId = "synthetic", Title = b.Title, Authors = b.Authors, Description = b.Description, Status = ReadingStatus.Unread }, 12000);
    private static string[] Inputs(Corpus c) => c.Books.Select(BookText).Concat(c.Queries.Select(q => q.Prompt)).Distinct().ToArray();
    private static RecordedVectorsTests.Recording ReadRecording(string file) => JsonSerializer.Deserialize<RecordedVectorsTests.Recording>(File.ReadAllText(PathFor(file)), Json)!;
    private static void CheckHeader(RecordedVectorsTests.Recording r) { Assert.Equal("text-embedding-3-large", r.Model); Assert.Equal(1536, r.Dimensions); Assert.Equal(IndexedBookText.Version, r.InputTemplateVersion); }

    [Fact]
    public void CorpusIsCompleteAndSplitIsDisjoint()
    {
        var c = ReadCorpus(); Assert.Equal(1, c.Version); Assert.Equal(36, c.Books.Length); Assert.Equal(36, c.Queries.Length);
        Assert.Equal(c.Books.Length, c.Books.Select(b => b.Id).Distinct().Count());
        Assert.Empty(c.Queries.Where(q => q.Split == "calibration").Select(q => q.IntentGroup).Intersect(c.Queries.Where(q => q.Split == "held-out").Select(q => q.IntentGroup)));
        Assert.True(c.Queries.Count(q => q.Split == "held-out") >= 18);
        Assert.True(c.Queries.Where(q => q.Split == "held-out").Sum(q => q.Relevant.Length) >= 30);
        foreach (var q in c.Queries)
            Assert.Equal(c.Books.Select(b => b.Id).Order(), q.Relevant.Concat(q.Borderline).Concat(q.Irrelevant).Order());
    }

    // This test can only create a recording through an explicit one-time opt-in.
    [Fact]
    public async Task RecordSyntheticInputsOnlyWhenExplicitlyRequested()
    {
        if (Environment.GetEnvironmentVariable("RECORD_THEMATIC_VECTORS") != "1") return;
        var corpus = ReadCorpus(); var old = ReadRecording("explore-vectors.json"); CheckHeader(old);
        var output = PathFor("thematic-vectors.json");
        Assert.False(File.Exists(output), "Recording already exists; do not start another recording.");
        var attemptsFile = PathFor("thematic-attempts.txt");
        Assert.False(File.Exists(attemptsFile), "A recording was already started; do not retry it.");
        var inputs = Inputs(corpus); var vectors = new Dictionary<string, string>();
        foreach (var input in inputs) if (old.Vectors.TryGetValue(RecordedVectorsTests.Hash(input), out var v)) vectors.Add(RecordedVectorsTests.Hash(input), v);
        Assert.True(inputs.Length - vectors.Count <= 72, "New inputs exceed the approved request cap.");
        var config = new ConfigurationBuilder().AddUserSecrets(typeof(ApiControllerBase).Assembly, optional: true).AddEnvironmentVariables().Build();
        var key = config["Ai:OpenAiApiKey"]; Assert.False(string.IsNullOrWhiteSpace(key), "OpenAI key unavailable.");
        using var handler = new CountingHandler(attemptsFile) { InnerHandler = new HttpClientHandler() };
        using var http = new HttpClient(handler);
        var opts = new OpenAIClientOptions { RetryPolicy = new ClientRetryPolicy(0), Transport = new HttpClientPipelineTransport(http) };
        var options = Options.Create(new ExploreOptions());
        var provider = new OpenAiEmbeddingProvider(options, new EmbeddingClient(options.Value.Model, new ApiKeyCredential(key!), opts));
        await File.WriteAllTextAsync(attemptsFile, "0", TestContext.Current.CancellationToken);
        foreach (var input in inputs)
        {
            var hash = RecordedVectorsTests.Hash(input); if (vectors.ContainsKey(hash)) continue;
            var vector = await provider.GenerateAsync(input, TestContext.Current.CancellationToken);
            Assert.NotNull(vector); Assert.Equal(1536, vector.Length);
            var bytes = new byte[vector.Length * sizeof(float)]; Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
            vectors.Add(hash, Convert.ToBase64String(bytes));
            var partial = new RecordedVectorsTests.Recording(options.Value.Model, 1536, IndexedBookText.Version, DateTime.UtcNow.ToString("yyyy-MM-dd"), vectors);
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(partial, Json), TestContext.Current.CancellationToken);
        }
    }
    private sealed class CountingHandler(string path) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var count = int.Parse(await File.ReadAllTextAsync(path, cancellationToken));
            if (count >= 72) throw new InvalidOperationException("Approved outbound attempt cap reached.");
            await File.WriteAllTextAsync(path, (count + 1).ToString(), cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }

    [Fact]
    public async Task ReplayThroughAuthenticatedExplore()
    {
        var split = Environment.GetEnvironmentVariable("THEMATIC_SPLIT") ?? "calibration";
        Assert.Contains(split, new[] { "calibration", "held-out" });
        var cutoff = double.Parse(Environment.GetEnvironmentVariable("THEMATIC_CUTOFF") ?? "0.7285", System.Globalization.CultureInfo.InvariantCulture);
        var corpus = ReadCorpus(); var recording = ReadRecording("thematic-vectors.json"); CheckHeader(recording);
        foreach (var input in Inputs(corpus)) Assert.True(recording.Vectors.ContainsKey(RecordedVectorsTests.Hash(input)), "Recording incomplete or input drifted.");
        var provider = new ReplayProvider(recording);
        await mongo.ClearBooksAsync(); await mongo.SeedDefaultUserAsync();
        await mongo.Database.GetCollection<UserEntity>(UserEntity.CollectionName).UpdateOneAsync(u => u.Id == DefaultTestUsers.Authenticated.Id, Builders<UserEntity>.Update.Set(u => u.IsPro, true), cancellationToken: TestContext.Current.CancellationToken);
        var ids = corpus.Books.ToDictionary(b => b.Id, _ => ObjectId.GenerateNewId().ToString());
        var books = corpus.Books.Select(b => new BookEntity { Id = ids[b.Id], UserId = ObjectId.Parse(DefaultTestUsers.Authenticated.Id), Title = b.Title, Authors = b.Authors, Description = b.Description, Embedding = provider.Find(BookText(b)), CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Status = ReadingStatus.Unread }).ToArray();
        await mongo.SeedBooksAndWaitForIndexing(books);
        await using var factory = new TestWebApplicationFactory(mongo, azurite).WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IEmbeddingProvider>(); s.AddSingleton<IEmbeddingProvider>(provider); s.PostConfigure<ExploreOptions>(o => o.RelevanceCutoff = cutoff); }));
        using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Explore-User-Id", DefaultTestUsers.Authenticated.Id);
        var results = new Dictionary<string, string[]>();
        foreach (var query in corpus.Queries.Where(q => q.Split == split))
        {
            using var response = await client.PostAsJsonAsync("/api/books/explore", new { prompt = query.Prompt }, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode(); var result = (await response.Content.ReadFromJsonAsync<ExploreResult>(TestContext.Current.CancellationToken))!;
            Assert.False(result.Truncated); results.Add(query.Id, result.Items.Select(b => ids.Single(pair => pair.Value == b.Id).Key).ToArray());
        }
        var destination = Environment.GetEnvironmentVariable("THEMATIC_OUTPUT");
        if (destination is not null)
            await File.WriteAllTextAsync(destination, JsonSerializer.Serialize(results, Json), TestContext.Current.CancellationToken);
        else
        {
            Assert.Equal("calibration", split);
            Assert.Equal(0.7285, cutoff);
            var baseline = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(PathFor("thematic-baseline-results.json")), Json)!;
            Assert.Equal(baseline.Keys.Order(), results.Keys.Order());
            foreach (var (id, expected) in baseline)
                Assert.Equal(expected.Order(), results[id].Order());
        }
    }
    private sealed class ReplayProvider(RecordedVectorsTests.Recording recording) : IEmbeddingProvider
    {
        public string ModelId => recording.Model;
        public float[] Find(string text)
        {
            if (!recording.Vectors.TryGetValue(RecordedVectorsTests.Hash(text), out var encoded)) throw new InvalidOperationException("Missing vector.");
            var bytes = Convert.FromBase64String(encoded); var vector = new float[bytes.Length / sizeof(float)]; Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length); return vector;
        }
        public Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken) => Task.FromResult<float[]?>(Find(text));
    }
}
