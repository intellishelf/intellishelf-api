using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Intellishelf.Api.Controllers;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Explore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Xunit;

namespace Intellishelf.Integration.Tests;

public sealed class RecordedVectorsTests
{
    private static readonly string Dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    internal record CorpusBook(string Title, string[] Authors, string Description);
    internal record Prompt(string Text, string[] Relevant);
    internal record Corpus(CorpusBook[] Books, Prompt[] Prompts);
    internal record Recording(string Model, int Dimensions, string InputTemplateVersion, string RecordedOn, Dictionary<string, string> Vectors);
    private static string PathFor(string name) => Path.Combine(Dir, name);
    internal static Corpus ReadCorpus() => JsonSerializer.Deserialize<Corpus>(File.ReadAllText(PathFor("explore-corpus.json")), JsonOptions)!;
    internal static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    internal static IEnumerable<string> Inputs(Corpus c) => c.Books.Select(b => IndexedBookText.Build(new AddBookRequest { UserId = "synthetic", Title = b.Title, Authors = b.Authors, Description = b.Description, Status = ReadingStatus.Unread }, 12000)).Concat(c.Prompts.Select(p => p.Text));
    [Fact]
    public async Task RecordOnlyWhenExplicitlyRequested()
    {
        if (Environment.GetEnvironmentVariable("RECORD_EXPLORE_VECTORS") != "1") return;
        var config = new ConfigurationBuilder().AddUserSecrets(typeof(ApiControllerBase).Assembly, optional: true).AddEnvironmentVariables().Build();
        var key = config["Ai:OpenAiApiKey"];
        Assert.False(string.IsNullOrWhiteSpace(key), "OpenAI key is unavailable.");
        var options = Options.Create(new ExploreOptions());
        var provider = new OpenAiEmbeddingProvider(options, new EmbeddingClient(options.Value.Model, key));
        var vectors = new Dictionary<string, string>();
        foreach (var input in Inputs(ReadCorpus()).Distinct())
        {
            var vector = await provider.GenerateAsync(input, TestContext.Current.CancellationToken);
            Assert.NotNull(vector);
            Assert.Equal(1536, vector.Length);
            var bytes = new byte[vector.Length * sizeof(float)];
            Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
            vectors.Add(Hash(input), Convert.ToBase64String(bytes));
        }
        var recording = new Recording(options.Value.Model, 1536, IndexedBookText.Version, DateTime.UtcNow.ToString("yyyy-MM-dd"), vectors);
        await File.WriteAllTextAsync(PathFor("explore-vectors.json"), JsonSerializer.Serialize(recording, JsonOptions), TestContext.Current.CancellationToken);
    }
    [Fact]
    public void RecordedVectorsMatchEveryCurrentInput()
    {
        var recording = JsonSerializer.Deserialize<Recording>(File.ReadAllText(PathFor("explore-vectors.json")), JsonOptions)!;
        Assert.Equal(IndexedBookText.Version, recording.InputTemplateVersion);
        Assert.Equal(1536, recording.Dimensions);
        foreach (var input in Inputs(ReadCorpus()))
            Assert.True(recording.Vectors.ContainsKey(Hash(input)), "Re-record the fixtures.");
    }
    internal static Dictionary<string, float[]> ReadVectors()
    {
        var recording = JsonSerializer.Deserialize<Recording>(File.ReadAllText(PathFor("explore-vectors.json")), JsonOptions)!;
        return recording.Vectors.ToDictionary(pair => pair.Key, pair =>
        {
            var bytes = Convert.FromBase64String(pair.Value);
            var vector = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
            return vector;
        });
    }
    [Fact]
    public void CutoffRetainsReferenceRelevantAndRejectsReferenceUnrelated()
    {
        var corpus = ReadCorpus(); var vectors = ReadVectors();
        var inputs = Inputs(corpus).ToArray();
        double Score(float[] a, float[] b)
        {
            double dot = 0, aa = 0, bb = 0;
            for (var i = 0; i < a.Length; i++) { dot += (double)a[i] * b[i]; aa += (double)a[i] * a[i]; bb += (double)b[i] * b[i]; }
            return (1 + dot / Math.Sqrt(aa * bb)) / 2;
        }
        var scores = corpus.Prompts.SelectMany((p, pi) => corpus.Books.Select((b, bi) => new
        {
            Prompt = p.Text,
            Book = b.Title,
            Relevant = p.Relevant.Contains(b.Title),
            Score = Score(vectors[Hash(inputs[corpus.Books.Length + pi])], vectors[Hash(inputs[bi])])
        })).ToArray();
        var cutoff = new ExploreOptions().RelevanceCutoff;
        Assert.All(scores.Where(x => x.Prompt == "books about loneliness and isolation" && x.Relevant), x => Assert.True(x.Score >= cutoff));
        Assert.All(scores.Where(x => x.Prompt == "books about quantum physics"), x => Assert.True(x.Score < cutoff));
        Assert.InRange(scores.Where(x => x.Relevant).Min(x => x.Score), 0.6514, 0.6517);
        Assert.InRange(scores.Where(x => !x.Relevant).Max(x => x.Score), 0.6971, 0.6974);
    }

}
