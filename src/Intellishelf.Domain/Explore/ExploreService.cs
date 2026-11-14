using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Users.DataAccess;
using Microsoft.Extensions.Options;

namespace Intellishelf.Domain.Explore;

public record ExploreResult(IReadOnlyList<Book> Items, bool Truncated);
public record ExploreCandidate(string Id, double Score);
public record ExploreBook(Book Book, string[] Authors);
public record ExploreMatches(IReadOnlyList<ExploreCandidate> TitleAuthor, IReadOnlyList<ExploreCandidate> Keyword, IReadOnlyList<ExploreCandidate> Vector);

public interface IExploreDao
{
    Task<ExploreMatches> SearchAsync(string userId, string[] words, float[] vector, int bound, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExploreBook>> LoadAsync(string userId, IEnumerable<string> ids, CancellationToken cancellationToken);
}

public sealed class ExploreService(IUserDao users, IEmbeddingProvider provider, IExploreDao dao, IOptions<ExploreOptions> options)
{
    public async Task<TryResult<ExploreResult>> SearchAsync(string userId, string? prompt, CancellationToken cancellationToken)
    {
        try
        {
            var user = await users.TryFindByIdAsync(userId);
            if (!user.IsSuccess || !user.Value.IsPro) return new Error("Explore.ProRequired", "Explore requires Pro.");
        }
        catch (Exception) { return new Error("Explore.ProRequired", "Explore requires Pro."); }
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > config.PromptLimit)
            return new Error("Explore.InvalidPrompt", "Enter a valid Explore prompt.");
        prompt = prompt.Trim();
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(config.SearchTimeoutSeconds));
            var vector = await provider.GenerateAsync(prompt, deadline.Token).WaitAsync(deadline.Token);
            if (vector is null || vector.Length != config.Dimensions || vector.Any(v => !float.IsFinite(v)) || vector.All(v => v == 0))
                return new Error("Explore.Unavailable", "AI search is unavailable right now.");
            var words = prompt.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var matches = await dao.SearchAsync(userId, words, vector, config.SafetyBound, deadline.Token).WaitAsync(deadline.Token);
            var vectors = matches.Vector.Where(x => x.Score >= config.RelevanceCutoff)
                .OrderByDescending(x => Math.Round(x.Score, 4)).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
            var ids = matches.TitleAuthor.Concat(matches.Keyword).Concat(vectors).Select(x => x.Id).Distinct().ToArray();
            var books = await dao.LoadAsync(userId, ids, deadline.Token).WaitAsync(deadline.Token);
            var titleIds = matches.TitleAuthor.Select(x => x.Id).ToHashSet();
            var keywordRanks = matches.Keyword.Select((x, i) => (x.Id, Rank: i + 1)).ToDictionary(x => x.Id, x => x.Rank);
            var vectorRanks = vectors.Select((x, i) => (x.Id, Rank: i + 1, Score: Math.Round(x.Score, 4))).ToDictionary(x => x.Id);
            var ordered = books.Select(x => new
            {
                x.Book,
                Tier = string.Equals(x.Book.Title, prompt, StringComparison.OrdinalIgnoreCase) || x.Authors.Any(a => string.Equals(a, prompt, StringComparison.OrdinalIgnoreCase)) ? 0 : titleIds.Contains(x.Book.Id) ? 1 : 2,
                Score = (keywordRanks.TryGetValue(x.Book.Id, out var kr) ? config.KeywordWeight / (60 + kr) : 0) + (vectorRanks.TryGetValue(x.Book.Id, out var vr) ? config.VectorWeight / (60 + vr.Rank) : 0),
                VectorScore = vectorRanks.TryGetValue(x.Book.Id, out var v) ? v.Score : 0
            }).OrderBy(x => x.Tier).ThenByDescending(x => x.Score).ThenByDescending(x => x.VectorScore).ThenBy(x => x.Book.Id, StringComparer.Ordinal).ToList();
            var truncated = matches.TitleAuthor.Count > config.SafetyBound || matches.Keyword.Count > config.SafetyBound || matches.Vector.Count >= config.SafetyBound || ordered.Count > config.SafetyBound;
            return new ExploreResult(ordered.Take(config.SafetyBound).Select(x => x.Book).ToList(), truncated);
        }
        catch (Exception) { return new Error("Explore.Unavailable", "AI search is unavailable right now."); }
    }
}
