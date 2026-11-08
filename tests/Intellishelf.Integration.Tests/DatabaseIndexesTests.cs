using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Indexes;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public class DatabaseIndexesTests(MongoDbFixture fixture)
{
    [Fact]
    public async Task Ensure_is_repeatable_and_enforces_unique_email()
    {
        var ct = TestContext.Current.CancellationToken;
        var books = fixture.Database.GetCollection<BookEntity>(BookEntity.CollectionName);
        var before = await (await books.SearchIndexes.ListAsync(cancellationToken: ct)).ToListAsync(ct);
        var version = Assert.Single(before, index => index["name"] == "default")["latestVersion"];
        await DatabaseIndexes.EnsureAsync(fixture.Database, ct);
        var searchIndexes = await (await books.SearchIndexes.ListAsync(cancellationToken: ct)).ToListAsync(ct);
        Assert.Equal(version, Assert.Single(searchIndexes, index => index["name"] == "default")["latestVersion"]);
        Assert.Single(searchIndexes, index => index["name"] == "default");
        var indexes = await (await fixture.Database.GetCollection<UserEntity>(UserEntity.CollectionName).Indexes.ListAsync(cancellationToken: ct)).ToListAsync(ct);
        Assert.Contains(indexes, index => index["key"].AsBsonDocument.Contains("Email") && index.GetValue("unique", false).ToBoolean());
        var bookIndexes = await (await books.Indexes.ListAsync(cancellationToken: ct)).ToListAsync(ct);
        Assert.Contains(bookIndexes, index => index["key"].AsBsonDocument.Contains("UserId"));
        var tokenIndexes = await (await fixture.Database.GetCollection<RefreshTokenEntity>(RefreshTokenEntity.CollectionName).Indexes.ListAsync(cancellationToken: ct)).ToListAsync(ct);
        Assert.Contains(tokenIndexes, index => index["key"].AsBsonDocument.Contains("Token"));

        var users = fixture.Database.GetCollection<BsonDocument>(UserEntity.CollectionName);
        var email = $"index-test-{Guid.NewGuid()}@example.com";
        try
        {
            await users.InsertOneAsync(new BsonDocument("Email", email), cancellationToken: ct);
            var error = await Assert.ThrowsAsync<MongoWriteException>(() =>
                users.InsertOneAsync(new BsonDocument("Email", email), cancellationToken: ct));
            Assert.Equal(ServerErrorCategory.DuplicateKey, error.WriteError.Category);
        }
        finally
        {
            await users.DeleteManyAsync(new BsonDocument("Email", email), ct);
        }
    }
}
