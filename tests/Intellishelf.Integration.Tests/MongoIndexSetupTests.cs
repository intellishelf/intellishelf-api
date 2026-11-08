using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Indexes;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public class MongoIndexSetupTests(MongoDbFixture fixture)
{
    [Fact]
    public async Task Ensure_CreatesIndexesAndDoesNotRebuildAnUnchangedSearchIndex()
    {
        var database = fixture.Database;
        var books = database.GetCollection<BookEntity>(BookEntity.CollectionName);
        var before = (await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken)).Single(x => x["name"] == "default");
        await MongoIndexSetup.EnsureAsync(database, TestContext.Current.CancellationToken);
        var after = (await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken)).Single(x => x["name"] == "default");
        Assert.Equal(before["latestVersion"], after["latestVersion"]);

        var bookIndexes = await (await books.Indexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Contains(bookIndexes, x => x["key"].AsBsonDocument.Contains("UserId"));
        var users = database.GetCollection<UserEntity>(UserEntity.CollectionName);
        var userIndexes = await (await users.Indexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Contains(userIndexes, x => x["key"].AsBsonDocument.Contains("Email") && x.GetValue("unique", false).ToBoolean());
        var tokens = database.GetCollection<RefreshTokenEntity>(RefreshTokenEntity.CollectionName);
        var tokenIndexes = await (await tokens.Indexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Contains(tokenIndexes, x => x["key"].AsBsonDocument.Contains("Token"));
    }

    [Fact]
    public async Task Ensure_UpdatesAnOutdatedSearchDefinition()
    {
        var database = new MongoClient(fixture.ConnectionString).GetDatabase("index-update-" + ObjectId.GenerateNewId());
        try
        {
            var books = database.GetCollection<BookEntity>(BookEntity.CollectionName);
            await books.Indexes.CreateOneAsync(new CreateIndexModel<BookEntity>(Builders<BookEntity>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: TestContext.Current.CancellationToken);
            await books.SearchIndexes.CreateOneAsync(new CreateSearchIndexModel("default", BsonDocument.Parse("{ mappings: { dynamic: true } }")), TestContext.Current.CancellationToken);
            await MongoIndexSetup.EnsureAsync(database, TestContext.Current.CancellationToken);
            var index = (await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken)).Single(x => x["name"] == "default");
            Assert.Equal("READY", index["status"].AsString);
            Assert.Equal("number", index["latestDefinition"]["mappings"]["fields"]["Status"]["type"].AsString);
        }
        finally
        {
            await database.Client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, TestContext.Current.CancellationToken);
        }
    }
}
