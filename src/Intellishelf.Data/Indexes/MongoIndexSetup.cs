using System.Diagnostics;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
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

        await EnsureSearchIndexAsync(books, BookSearchIndexes.Text(), cancellationToken);
        await EnsureSearchIndexAsync(books, BookSearchIndexes.Vector(), cancellationToken);
    }

    private static async Task EnsureSearchIndexAsync(
        IMongoCollection<BookEntity> books, CreateSearchIndexModel model, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var created = false;

        while (stopwatch.Elapsed < TimeSpan.FromMinutes(2))
        {
            try
            {
                using var cursor = await books.SearchIndexes.ListAsync(cancellationToken: cancellationToken);
                var indexes = await cursor.ToListAsync(cancellationToken);
                var index = indexes.FirstOrDefault(i => i.GetValue("name", "") == model.Name);

                if (index is null && !created)
                {
                    await books.SearchIndexes.CreateOneAsync(model, cancellationToken);
                    created = true;
                }
                else if (index is not null && index.GetValue("status", "") == "READY")
                {
                    return;
                }
            }
            catch (MongoCommandException ex) when (ex.Message.Contains("connecting to Search Index Management service") || ex.Message.Contains("mongot is shutting down"))
            {
                // Atlas Local starts its search service separately from MongoDB.
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException($"Search index {model.Name} did not become ready within two minutes.");
    }
}
