using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Integration.Tests.Infra;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Intellishelf.Integration.Tests.Infra.Fixtures;

public class MongoDbFixture : IAsyncLifetime
{
    private MongoDbContainer _mongoContainer;
    public string ConnectionString => _mongoContainer.GetConnectionString();
    public IMongoDatabase Database { get; private set; }

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();

        await _mongoContainer.StartAsync();

        var client = new MongoClient(_mongoContainer.GetConnectionString());
        Database = client.GetDatabase("intellishelf-test");
    }

    public async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }

    private Task SeedUserAsync(UserEntity user)
    {
        var collection = Database.GetCollection<UserEntity>(UserEntity.CollectionName);
        return collection.ReplaceOneAsync(
            u => u.Id == user.Id,
            user,
            new ReplaceOptions { IsUpsert = true });
    }

    public Task SeedDefaultUserAsync() => SeedUserAsync(DefaultTestUsers.Authenticated.ToEntity());

    public async Task SeedBooksAsync(params BookEntity[] books)
    {
        if (books.Length == 0)
        {
            return;
        }

        var collection = Database.GetCollection<BookEntity>(BookEntity.CollectionName);
        await collection.InsertManyAsync(books);
    }

    public Task ClearBooksAsync()
    {
        var books = Database.GetCollection<BookEntity>(BookEntity.CollectionName);
        return books.DeleteManyAsync(FilterDefinition<BookEntity>.Empty);
    }
}
