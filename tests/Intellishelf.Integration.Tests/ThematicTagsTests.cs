using System.ClientModel;
using System.ClientModel.Primitives;
using System.Security.Cryptography;
using System.Text.Json;
using Intellishelf.Api.Controllers;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Explore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;
using Xunit;

namespace Intellishelf.Integration.Tests;

public sealed class ThematicTagsTests
{
    private static readonly string Dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, WriteIndented = true };
    private sealed record Book(string Id, string Title, string[] Authors, string? Description);
    private sealed record Query(string Id, string Prompt, string Split);
    private sealed record Corpus(Book[] Books, Query[] Queries);
    private sealed record Spec(Dictionary<string, string[]> Tags);
    private sealed record VectorFile(string Model, int Dimensions, string InputTemplateVersion, Dictionary<string, string> Vectors);
    private static string PathFor(string name) => Path.Combine(Dir, name);
    private static byte[] ReadFrozen(string file, string hash)
    {
        var bytes = File.ReadAllBytes(PathFor(file)); Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(bytes))); return bytes;
    }
    private static (Corpus Corpus, Spec Spec) ReadInputs()
    {
        var corpus = JsonSerializer.Deserialize<Corpus>(ReadFrozen("thematic-corpus.json", "0D36FAE51AC228F2E7A1B4913086D56D4B25FA8614F8FB919391FDAF9C128CC3"), Json)!;
        var spec = JsonSerializer.Deserialize<Spec>(ReadFrozen("thematic-tag-spec.json", "4FA6D4CBBF0FB2FA671A9F915609C06785D93BF54958F471D7C5A7496C9B5BBD"), Json)!;
        Assert.Equal(corpus.Books.Select(b => b.Id).Order(), spec.Tags.Keys.Order()); Assert.All(spec.Tags.Values, t => Assert.Equal(3, t.Length));
        return (corpus, spec);
    }
    private static string BookText(Book b, Spec spec) => IndexedBookText.Build(new AddBookRequest { UserId = "synthetic", Title = b.Title, Authors = b.Authors, Description = b.Description, Tags = spec.Tags[b.Id], Status = ReadingStatus.Unread }, 12000);
    private static string[] Inputs(Corpus c, Spec s) => c.Books.Select(b => BookText(b, s)).Distinct().ToArray();
    [Fact]
    public void FrozenTagInputsAreBounded()
    {
        var (c, s) = ReadInputs(); Assert.Equal(18, c.Queries.Count(q => q.Split == "calibration")); Assert.Equal(36, Inputs(c, s).Length);
    }
    [Fact]
    public async Task RecordTagVectorsOnlyWhenExplicitlyRequested()
    {
        if (Environment.GetEnvironmentVariable("RECORD_THEMATIC_TAGS") != "1") return;
        var (corpus, spec) = ReadInputs(); var inputs = Inputs(corpus, spec); Assert.True(inputs.Length <= 36);
        var old = JsonSerializer.Deserialize<RecordedVectorsTests.Recording>(File.ReadAllText(PathFor("thematic-vectors.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal("text-embedding-3-large", old.Model); Assert.Equal(1536, old.Dimensions); Assert.Equal(IndexedBookText.Version, old.InputTemplateVersion);
        var attemptsPath = PathFor("thematic-tag-attempts.json"); var outputPath = PathFor("thematic-tag-vectors.json");
        var attempts = File.Exists(attemptsPath) ? JsonSerializer.Deserialize<List<string>>(File.ReadAllText(attemptsPath))! : new List<string>();
        Assert.True(attempts.Count <= 36 && attempts.Distinct().Count() == attempts.Count);
        if (!File.Exists(attemptsPath)) await File.WriteAllTextAsync(attemptsPath, "[]", TestContext.Current.CancellationToken);
        var saved = File.Exists(outputPath) ? JsonSerializer.Deserialize<VectorFile>(File.ReadAllText(outputPath), Json) : new VectorFile(old.Model, 1536, IndexedBookText.Version, new Dictionary<string, string>());
        Assert.NotNull(saved); Assert.Equal(old.Model, saved.Model); Assert.Equal(1536, saved.Dimensions); Assert.Equal(IndexedBookText.Version, saved.InputTemplateVersion);
        foreach (var input in inputs) { var hash = RecordedVectorsTests.Hash(input); if (!saved.Vectors.ContainsKey(hash) && old.Vectors.TryGetValue(hash, out var existing)) saved.Vectors.Add(hash, existing); }
        Assert.True(inputs.Count(i => !saved.Vectors.ContainsKey(RecordedVectorsTests.Hash(i)) && !attempts.Contains(RecordedVectorsTests.Hash(i))) <= 36 - attempts.Count);
        var config = new ConfigurationBuilder().AddUserSecrets(typeof(ApiControllerBase).Assembly, optional: true).AddEnvironmentVariables().Build();
        var key = config["Ai:OpenAiApiKey"]; Assert.False(string.IsNullOrWhiteSpace(key), "OpenAI key unavailable.");
        using var handler = new CountingHandler(attemptsPath) { InnerHandler = new HttpClientHandler() }; using var http = new HttpClient(handler);
        var client = new EmbeddingClient(old.Model, new ApiKeyCredential(key!), new OpenAIClientOptions { RetryPolicy = new ClientRetryPolicy(0), Transport = new HttpClientPipelineTransport(http) });
        var provider = new OpenAiEmbeddingProvider(Options.Create(new ExploreOptions()), client);
        foreach (var input in inputs)
        {
            var hash = RecordedVectorsTests.Hash(input); if (saved.Vectors.ContainsKey(hash) || attempts.Contains(hash)) continue;
            handler.InputHash = hash;
            var vector = await provider.GenerateAsync(input, TestContext.Current.CancellationToken);
            Assert.NotNull(vector); Assert.Equal(1536, vector.Length);
            var bytes = new byte[vector.Length * sizeof(float)]; Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
            saved.Vectors.Add(hash, Convert.ToBase64String(bytes));
            await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(saved, Json), TestContext.Current.CancellationToken);
        }
        Assert.All(inputs, input => Assert.True(saved.Vectors.ContainsKey(RecordedVectorsTests.Hash(input)), "A prior failed attempt cannot be retried."));
    }
    private sealed class CountingHandler(string path) : DelegatingHandler
    {
        public string InputHash { get; set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var attempts = JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(path, cancellationToken))!;
            if (attempts.Count >= 36 || attempts.Contains(InputHash)) throw new InvalidOperationException("Tag request cap or repeated input.");
            attempts.Add(InputHash); await File.WriteAllTextAsync(path, JsonSerializer.Serialize(attempts), cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
