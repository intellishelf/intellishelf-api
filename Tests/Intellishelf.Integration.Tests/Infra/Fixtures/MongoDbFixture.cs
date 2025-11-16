using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace Intellishelf.Integration.Tests.Infra.Fixtures;

public class MongoDbFixture : IAsyncLifetime
{
    private const int MongoDbPort = 27017;
    private IContainer _container = default!;
    public string ConnectionString { get; private set; } = default!;
    public IMongoDatabase Database { get; private set; } = default!;
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("mongodb/mongodb-atlas-local:latest")
            // map container port to a random free host port
            .WithPortBinding(MongoDbPort, assignRandomHostPort: true)
            // wait until MongoDB inside the container is listening
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(MongoDbPort)
            )
            .Build();

        await _container.StartAsync();

        var mappedPort = _container.GetMappedPublicPort(MongoDbPort);

        // Recommended pattern from Atlas Local docs:
        // mongosh "mongodb://localhost:27017/?directConnection=true"
        // :contentReference[oaicite:2]{index=2}
        ConnectionString = $"mongodb://127.0.0.1:{mappedPort}/?directConnection=true";

        var client = new MongoClient(ConnectionString);
        Database = client.GetDatabase("intellishelf-test");

        // Create Atlas Search index for books collection
        await CreateSearchIndexAsync();
    }

    private async Task CreateSearchIndexAsync()
    {
        // Ensure the collection exists by creating it if it doesn't
        try
        {
            await Database.CreateCollectionAsync(BookEntity.CollectionName);
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("already exists"))
        {
            // Collection already exists, that's fine
        }

        var booksCollection = Database.GetCollection<BookEntity>(BookEntity.CollectionName);
        var searchIndexJson = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Infra", "Fixtures", "search-index.json"));
        var searchIndexDefinition = BsonSerializer.Deserialize<BsonDocument>(searchIndexJson);

        const string indexName = "default";
        const int maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(1);
        var indexReadyTimeout = TimeSpan.FromMinutes(1);
        var pollInterval = TimeSpan.FromSeconds(1);

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Create the search index
                await booksCollection.SearchIndexes.CreateOneAsync(
                    new CreateSearchIndexModel(indexName, searchIndexDefinition));

                // Wait for index to become READY
                await WaitForIndexReadyAsync(booksCollection, indexName, indexReadyTimeout, pollInterval);
                return;
            }
            catch (MongoCommandException ex) when (ex.Message.Contains("connecting to Search Index Management service") && attempt < maxRetries - 1)
            {
                // Search service not ready, retry after delay
                await Task.Delay(retryDelay);
            }
        }

        throw new TimeoutException("Failed to create search index after multiple retries. Search service may not be available.");
    }

    private async Task WaitForIndexReadyAsync(IMongoCollection<BookEntity> collection, string indexName, TimeSpan timeout, TimeSpan pollInterval)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            var indexes = (await collection.SearchIndexes.ListAsync()).ToList();
            var index = indexes.FirstOrDefault(i => i["name"] == indexName);

            if (index != null && index.Contains("status") && index["status"] == "READY")
            {
                return; // Index is ready
            }

            await Task.Delay(pollInterval);
        }

        throw new TimeoutException("Search index did not become ready within the expected time.");
    }

    public async Task DisposeAsync() =>
        await _container.DisposeAsync();

    private Task SeedUserAsync(UserEntity user)
    {
        var collection = Database!.GetCollection<UserEntity>(UserEntity.CollectionName);
        return collection.ReplaceOneAsync(
            u => u.Id == user.Id,
            user,
            new ReplaceOptions { IsUpsert = true });
    }

    public Task SeedDefaultUserAsync() => SeedUserAsync(DefaultTestUsers.Authenticated.ToEntity());

    public async Task SeedBooksAsync(params BookEntity[] books) =>
        await Database.GetCollection<BookEntity>(BookEntity.CollectionName).InsertManyAsync(books);

    /// <summary>
    /// Seeds books and waits for Atlas Search indexing to complete.
    /// Use this method ONLY for search tests. Regular tests should use SeedBooksAsync().
    /// </summary>
    public async Task SeedBooksAndWaitForIndexing(params BookEntity[] books)
    {
        await SeedBooksAsync(books);
        // Atlas Search has ~1 second indexing delay regardless of document count
        await Task.Delay(1500);
    }

    public async Task ClearBooksAsync() =>
        await Database.GetCollection<BookEntity>(BookEntity.CollectionName).DeleteManyAsync(FilterDefinition<BookEntity>.Empty);

    public async Task<BookEntity?> FindBookByIdAsync(string bookId) =>
        await Database.GetCollection<BookEntity>(BookEntity.CollectionName).Find(b => b.Id == bookId).FirstOrDefaultAsync();
}