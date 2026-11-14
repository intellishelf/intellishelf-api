using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Users.DataAccess;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace Intellishelf.Domain.Explore;

public class ExploreOptions
{
    public const string SectionName = "Explore";
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = "text-embedding-3-large";
    public int Dimensions { get; set; } = 1536;
    public double RelevanceCutoff { get; set; } = 0.7;
    public int SafetyBound { get; set; } = 1000;
    public int PromptLimit { get; set; } = 500;
    public int EmbeddingTimeoutSeconds { get; set; } = 5;
    public int SearchTimeoutSeconds { get; set; } = 10;
    public double KeywordWeight { get; set; } = 1;
    public double VectorWeight { get; set; } = 1;
    public int InputCharacterBudget { get; set; } = 12000;
    public bool IsValid() => (Provider == "OpenAI" || Provider == "StandIn") && !string.IsNullOrWhiteSpace(Model)
        && Dimensions == 1536 && RelevanceCutoff is >= 0 and <= 1 && SafetyBound > 0 && PromptLimit > 0
        && EmbeddingTimeoutSeconds > 0 && SearchTimeoutSeconds > 0 && KeywordWeight > 0 && VectorWeight > 0
        && InputCharacterBudget is > 0 and <= 12000;
}

public interface IEmbeddingProvider
{
    string ModelId { get; }
    Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken);
}

public sealed class OpenAiEmbeddingProvider(IOptions<ExploreOptions> options, EmbeddingClient client) : IEmbeddingProvider
{
    public string ModelId => options.Value.Model;
    public async Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            var result = await client.GenerateEmbeddingAsync(text, new EmbeddingGenerationOptions { Dimensions = options.Value.Dimensions }, cancellationToken);
            return result.Value.ToFloats().ToArray();
        }
        catch (Exception) { return null; }
    }
}

public sealed class StandInEmbeddingProvider(IOptions<ExploreOptions> options) : IEmbeddingProvider
{
    public string ModelId => "stand-in-v1";
    public Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken)
    {
        var vector = new float[options.Value.Dimensions];
        foreach (Match match in Regex.Matches(text.ToLowerInvariant(), @"[\p{L}\p{N}]+"))
        {
            var word = match.Value switch
            {
                "loneliness" or "lonely" or "isolation" or "isolated" => "loneliness",
                "knowledge" or "learning" or "learn" => "knowledge",
                "science" or "scientific" => "science",
                _ => match.Value
            };
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(word));
            vector[BitConverter.ToUInt32(hash, 0) % (uint)vector.Length] += 1;
        }
        if (vector.All(v => v == 0)) vector[0] = 1;
        return Task.FromResult<float[]?>(vector);
    }
}

public record BookIndexState(float[]? Embedding, string? Hash, string? Model);
public record BookIndexUpdate(float[]? Embedding, string? Hash, string? Model);

public static class IndexedBookText
{
    public const string Version = "book-v1";
    public static string Build(BookRequestBase book, int budget)
    {
        static string Normalize(string? value) => Regex.Replace(value?.Trim() ?? "", @"\s+", " ");
        static string List(string[]? values) => string.Join(", ", (values ?? []).Select(Normalize));
        var text = $"Title: {Normalize(book.Title)}\nAuthors: {List(book.Authors)}\nTags: {List(book.Tags)}\nPublisher: {Normalize(book.Publisher)}\nDescription: {Normalize(book.Description)}\nAnnotation: {Normalize(book.Annotation)}";
        // Each UTF-8 byte can account for at most one token in a byte-based tokenizer.
        const int byteBudget = 8191;
        var result = new StringBuilder();
        var bytes = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (result.Length + rune.Utf16SequenceLength > budget || bytes + rune.Utf8SequenceLength > byteBudget)
                break;
            result.Append(rune);
            bytes += rune.Utf8SequenceLength;
        }
        return result.ToString();
    }
    public static string Hash(string text, string model, int dimensions) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes($"{Version}\n{model}\n{dimensions}\n{text}")));
}

public class BookIndexer(IUserDao users, IEmbeddingProvider provider, IOptions<ExploreOptions> options)
{
    public async Task<BookIndexUpdate> PrepareAsync(BookRequestBase book, BookIndexState? existing = null)
    {
        var config = options.Value;
        var text = IndexedBookText.Build(book, config.InputCharacterBudget);
        var hash = IndexedBookText.Hash(text, provider.ModelId, config.Dimensions);
        if (existing?.Embedding is not null && existing.Hash == hash)
            return new(existing.Embedding, existing.Hash, existing.Model);
        try
        {
            var user = await users.TryFindByIdAsync(book.UserId);
            if (!user.IsSuccess || !user.Value.IsPro) return new(null, null, null);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(config.EmbeddingTimeoutSeconds));
            var vector = await provider.GenerateAsync(text, deadline.Token).WaitAsync(deadline.Token);
            if (vector is null || vector.Length != config.Dimensions || vector.Any(v => !float.IsFinite(v)) || vector.All(v => v == 0))
                return new(null, null, null);
            return new(vector, hash, provider.ModelId);
        }
        catch (Exception) { return new(null, null, null); }
    }
}
