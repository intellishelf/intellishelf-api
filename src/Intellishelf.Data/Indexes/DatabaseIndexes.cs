using System.Diagnostics;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intellishelf.Data.Indexes;

/// <summary>Explicitly provisions indexes; callers decide which databases are safe to change.</summary>
public static class DatabaseIndexes
{
    private const string SearchIndexName = "default";
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    public static async Task EnsureAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var books = database.GetCollection<BookEntity>(BookEntity.CollectionName);
        var users = database.GetCollection<UserEntity>(UserEntity.CollectionName);
        var tokens = database.GetCollection<RefreshTokenEntity>(RefreshTokenEntity.CollectionName);

        await books.Indexes.CreateOneAsync(new CreateIndexModel<BookEntity>(
            Builders<BookEntity>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await users.Indexes.CreateOneAsync(new CreateIndexModel<UserEntity>(
            Builders<UserEntity>.IndexKeys.Ascending(x => x.Email), new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
        await tokens.Indexes.CreateOneAsync(new CreateIndexModel<RefreshTokenEntity>(
            Builders<RefreshTokenEntity>.IndexKeys.Ascending(x => x.Token)), cancellationToken: cancellationToken);

        var assembly = typeof(DatabaseIndexes).Assembly;
        using var stream = assembly.GetManifestResourceStream("Intellishelf.Data.Indexes.books-search-index.json")
            ?? throw new InvalidOperationException("Books search index definition is missing.");
        using var reader = new StreamReader(stream);
        var definition = BsonDocument.Parse(await reader.ReadToEndAsync(cancellationToken));
        var timer = Stopwatch.StartNew();
        bool submitted = false;
        while (timer.Elapsed < Timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var indexes = await (await books.SearchIndexes.ListAsync(cancellationToken: cancellationToken)).ToListAsync(cancellationToken);
                var index = indexes.FirstOrDefault(x => x.GetValue("name", "") == SearchIndexName);
                if (!submitted)
                {
                    if (index == null)
                    {
                        await books.SearchIndexes.CreateOneAsync(new CreateSearchIndexModel(SearchIndexName, definition), cancellationToken);
                        submitted = true;
                    }
                    else if (!index.TryGetValue("latestDefinition", out var existing) || !Equivalent(existing, definition))
                    {
                        await books.SearchIndexes.UpdateAsync(SearchIndexName, definition, cancellationToken);
                        submitted = true;
                    }
                    else
                    {
                        submitted = true;
                    }
                }
                else if (index != null && index.GetValue("status", "") == "READY" &&
                         index.TryGetValue("latestDefinition", out var latest) && Equivalent(latest, definition))
                {
                    return;
                }
            }
            catch (MongoNodeIsRecoveringException)
            {
                // Atlas Local can restart mongod during its initial replica-set setup.
            }
            catch (MongoCommandException ex) when (ex.Message.Contains("connecting to Search Index Management service", StringComparison.OrdinalIgnoreCase))
            {
                // Atlas Local's search management service can start after mongod.
            }
            await Task.Delay(PollInterval, cancellationToken);
        }
        throw new TimeoutException("The books search index did not become ready within two minutes.");
    }

    // Atlas returns implicit field defaults and may reorder the type mappings for a field.
    private static bool Equivalent(BsonValue actual, BsonValue expected)
    {
        if (actual.IsBsonDocument && expected.IsBsonDocument)
        {
            var a = actual.AsBsonDocument;
            var b = expected.AsBsonDocument;
            return b.All(e => a.TryGetValue(e.Name, out var value) && Equivalent(value, e.Value)) &&
                   a.All(e => b.Contains(e.Name) || IsImplicitFieldDefault(a, e));
        }
        if (actual.IsBsonArray && expected.IsBsonArray)
        {
            var remaining = actual.AsBsonArray.ToList();
            foreach (var item in expected.AsBsonArray)
            {
                var match = remaining.FindIndex(candidate => Equivalent(candidate, item));
                if (match < 0) return false;
                remaining.RemoveAt(match);
            }
            return remaining.Count == 0;
        }
        return actual.Equals(expected);
    }

    private static bool IsImplicitFieldDefault(BsonDocument field, BsonElement element)
    {
        var type = field.GetValue("type", "").AsString;
        return (type, element.Name) switch
        {
            ("string", "indexOptions") => element.Value == "offsets",
            ("string", "store") => element.Value == true,
            ("string", "norms") => element.Value == "include",
            ("autocomplete", "minGrams") => element.Value == 2,
            ("autocomplete", "maxGrams") => element.Value == 15,
            ("autocomplete", "foldDiacritics") => element.Value == true,
            ("autocomplete", "tokenization") => element.Value == "edgeGram",
            ("number", "representation") => element.Value == "double",
            ("number", "indexDoubles" or "indexIntegers") => element.Value == true,
            _ => false
        };
    }
}
