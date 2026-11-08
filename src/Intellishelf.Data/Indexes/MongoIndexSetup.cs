using System.Diagnostics;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intellishelf.Data.Indexes;

/// <summary>Explicitly provisions the indexes required by the application.</summary>
public static class MongoIndexSetup
{
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

        await EnsureSearchIndexAsync(books, cancellationToken);
    }

    private static async Task EnsureSearchIndexAsync(IMongoCollection<BookEntity> books, CancellationToken cancellationToken)
    {
        using var stream = typeof(MongoIndexSetup).Assembly.GetManifestResourceStream("Intellishelf.Data.Indexes.books-search-index.json")
            ?? throw new InvalidOperationException("Books search index definition is missing.");
        using var reader = new StreamReader(stream);
        var definition = BsonDocument.Parse(await reader.ReadToEndAsync(cancellationToken));
        var stopwatch = Stopwatch.StartNew();
        var updateSubmitted = false;

        while (stopwatch.Elapsed < TimeSpan.FromMinutes(2))
        {
            try
            {
                using var cursor = await books.SearchIndexes.ListAsync(cancellationToken: cancellationToken);
                var indexes = await cursor.ToListAsync(cancellationToken);
                var index = indexes.FirstOrDefault(i => i.GetValue("name", "") == "default");

                if (index is null && !updateSubmitted)
                {
                    await books.SearchIndexes.CreateOneAsync(new CreateSearchIndexModel("default", definition), cancellationToken);
                    updateSubmitted = true;
                }
                else if (index is not null)
                {
                    var matches = index.TryGetValue("latestDefinition", out var current) && Equivalent(current, definition);
                    if (matches && index.GetValue("status", "") == "READY")
                        return;

                    if (!matches && !updateSubmitted)
                    {
                        await books.SearchIndexes.UpdateAsync("default", definition, cancellationToken);
                        updateSubmitted = true;
                    }
                }
            }
            catch (MongoCommandException ex) when (ex.Message.Contains("connecting to Search Index Management service") || ex.Message.Contains("mongot is shutting down"))
            {
                // Atlas Local starts its search service separately from MongoDB.
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException("The books search index did not become ready within two minutes.");
    }

    // Atlas returns implicit field defaults and may reorder multi-type mappings.
    private static BsonValue Normalize(BsonValue value)
    {
        if (value.IsBsonArray)
            return new BsonArray(value.AsBsonArray.Select(Normalize).OrderBy(x => x.ToJson(), StringComparer.Ordinal));
        if (!value.IsBsonDocument)
            return value;

        var document = value.AsBsonDocument;
        var type = document.GetValue("type", "").ToString();
        var defaults = type switch
        {
            "string" => new BsonDocument { { "indexOptions", "offsets" }, { "store", true }, { "norms", "include" } },
            "autocomplete" => new BsonDocument { { "minGrams", 2 }, { "maxGrams", 15 }, { "foldDiacritics", true }, { "tokenization", "edgeGram" } },
            "number" => new BsonDocument { { "representation", "double" }, { "indexDoubles", true }, { "indexIntegers", true } },
            _ => new BsonDocument()
        };
        return new BsonDocument(document
            .Where(e => !defaults.TryGetValue(e.Name, out var defaultValue) || !e.Value.Equals(defaultValue))
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .Select(e => new BsonElement(e.Name, Normalize(e.Value))));
    }

    private static bool Equivalent(BsonValue left, BsonValue right) => Normalize(left).Equals(Normalize(right));
}
