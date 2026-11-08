using System.Diagnostics;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intellishelf.Data.Indexes;

/// <summary>Explicitly provisions the indexes required by the application.</summary>
public static class MongoIndexSetup
{
    public static async Task EnsureAsync(IMongoDatabase database, CancellationToken cancellationToken = default, bool recreateVectorIndexOnUpdateFailure = false, bool updateExisting = false)
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

        var textIndex = BookSearchIndexes.Text();
        var vectorIndex = BookSearchIndexes.Vector();
        await EnsureSearchIndexAsync(books, textIndex, textIndex.Definition, cancellationToken, updateExisting: updateExisting);
        await EnsureSearchIndexAsync(books, vectorIndex, BookSearchIndexes.VectorDefinition(vectorIndex), cancellationToken, recreateVectorIndexOnUpdateFailure, updateExisting);
    }

    private static async Task EnsureSearchIndexAsync(
        IMongoCollection<BookEntity> books, CreateSearchIndexModel model, BsonDocument definition,
        CancellationToken cancellationToken, bool recreateOnUpdateFailure = false, bool updateExisting = false)
    {
        var name = model.Name;
        var stopwatch = Stopwatch.StartNew();
        var updateSubmitted = false;
        var waitingForDrop = false;

        while (stopwatch.Elapsed < TimeSpan.FromMinutes(2))
        {
            try
            {
                using var cursor = await books.SearchIndexes.ListAsync(cancellationToken: cancellationToken);
                var indexes = await cursor.ToListAsync(cancellationToken);
                var index = indexes.FirstOrDefault(i => i.GetValue("name", "") == name);

                if (index is null && !updateSubmitted)
                {
                    await books.SearchIndexes.CreateOneAsync(model, cancellationToken);
                    updateSubmitted = true;
                    waitingForDrop = false;
                }
                else if (index is not null && !waitingForDrop)
                {
                    var matches = index.TryGetValue("latestDefinition", out var current) && Equivalent(current, definition);
                    if (matches && index.GetValue("status", "") == "READY")
                        return;

                    if (!matches && !updateSubmitted)
                    {
                        if (!updateExisting)
                            throw new InvalidOperationException($"Search index {name} differs from its configured definition. Update it before starting the application.");

                        try
                        {
                            await books.SearchIndexes.UpdateAsync(name, definition, cancellationToken);
                            updateSubmitted = true;
                        }
                        catch (MongoCommandException ex) when (recreateOnUpdateFailure && model.Type == SearchIndexType.VectorSearch && ex.Message.Contains("\"mappings\" is required"))
                        {
                            // Explicitly enabled for disposable local databases; book documents are retained.
                            await books.SearchIndexes.DropOneAsync(name, cancellationToken);
                            waitingForDrop = true;
                        }
                    }
                }
            }
            catch (MongoCommandException ex) when (ex.Message.Contains("connecting to Search Index Management service") || ex.Message.Contains("mongot is shutting down"))
            {
                // Atlas Local starts its search service separately from MongoDB.
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException($"Search index {name} did not become ready within two minutes.");
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
