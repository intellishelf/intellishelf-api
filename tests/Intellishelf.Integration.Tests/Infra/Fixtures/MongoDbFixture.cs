using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
using MongoDB.Bson;
using Intellishelf.Data.Indexes;
using MongoDB.Driver;
using Xunit;

namespace Intellishelf.Integration.Tests.Infra.Fixtures;

public class MongoDbFixture : IAsyncLifetime
{
    private const int MongoDbPort = 27017;
    private IContainer _container = default!;
    public string ConnectionString { get; private set; } = default!;
    public IMongoDatabase Database { get; private set; } = default!;
    public async ValueTask InitializeAsync()
    {
        _container = new ContainerBuilder("mongodb/mongodb-atlas-local:latest")
            // map container port to a random free host port
            .WithPortBinding(MongoDbPort, assignRandomHostPort: true)
            // wait until MongoDB inside the container is listening
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilInternalTcpPortIsAvailable(MongoDbPort)
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

        await DatabaseIndexes.EnsureAsync(Database);
    }

    public async ValueTask DisposeAsync() =>
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

    public async Task<bool> UserExistsAsync(string userId) =>
        await Database.GetCollection<UserEntity>(UserEntity.CollectionName)
            .Find(u => u.Id == userId)
            .AnyAsync();

    public async Task<List<BookEntity>> GetBooksByUserIdAsync(string userId)
    {
        var userIdObject = ObjectId.Parse(userId);
        return await Database.GetCollection<BookEntity>(BookEntity.CollectionName)
            .Find(b => b.UserId == userIdObject)
            .ToListAsync();
    }

    public async Task SeedRefreshTokenAsync(string userId, string token)
    {
        var entity = new RefreshTokenEntity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Token = token,
            UserId = userId,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await Database.GetCollection<RefreshTokenEntity>(RefreshTokenEntity.CollectionName)
            .InsertOneAsync(entity);
    }

    public async Task<List<RefreshTokenEntity>> GetRefreshTokensByUserIdAsync(string userId) =>
        await Database.GetCollection<RefreshTokenEntity>(RefreshTokenEntity.CollectionName)
            .Find(rt => rt.UserId == userId)
            .ToListAsync();

    public async Task ClearUsersAsync() =>
        await Database.GetCollection<UserEntity>(UserEntity.CollectionName).DeleteManyAsync(FilterDefinition<UserEntity>.Empty);

    public async Task ClearRefreshTokensAsync() =>
        await Database.GetCollection<RefreshTokenEntity>(RefreshTokenEntity.CollectionName).DeleteManyAsync(FilterDefinition<RefreshTokenEntity>.Empty);
}
