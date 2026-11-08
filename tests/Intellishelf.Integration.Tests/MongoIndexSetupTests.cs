using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Indexes;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Intellishelf.Integration.Tests.Infra;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public class MongoIndexSetupTests(MongoDbFixture fixture, AzuriteFixture azuriteFixture)
{
    [Fact]
    public async Task ApplicationStartup_CreatesIndexesOnEmptyDatabase()
    {
        var database = new MongoClient(fixture.ConnectionString).GetDatabase("startup-indexes-" + ObjectId.GenerateNewId());
        try
        {
            using var factory = new TestWebApplicationFactory(fixture, azuriteFixture, database);
            using var client = factory.CreateClient();
            var books = database.GetCollection<BookEntity>(BookEntity.CollectionName);
            var indexes = await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
            Assert.Contains(indexes, index => index["name"] == "default" && index["status"] == "READY");
            Assert.Contains(indexes, index => index["name"] == "vector_index" && index["status"] == "READY");
        }
        finally
        {
            await database.Client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ApplicationStartup_DoesNotReplaceAnOutdatedIndex()
    {
        var database = new MongoClient(fixture.ConnectionString).GetDatabase("startup-existing-" + ObjectId.GenerateNewId());
        try
        {
            var books = database.GetCollection<BookEntity>(BookEntity.CollectionName);
            await books.Indexes.CreateOneAsync(new CreateIndexModel<BookEntity>(Builders<BookEntity>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: TestContext.Current.CancellationToken);
            await books.SearchIndexes.CreateOneAsync(new CreateSearchIndexModel("default", BsonDocument.Parse("{ mappings: { dynamic: true } }")), TestContext.Current.CancellationToken);
            using var factory = new TestWebApplicationFactory(fixture, azuriteFixture, database);
            var error = Record.Exception(() => factory.CreateClient());
            Assert.NotNull(error);
            Assert.Contains("differs from its configured definition", error.ToString());
            var indexes = await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
            var index = Assert.Single(indexes, item => item["name"] == "default");
            Assert.True(index["latestDefinition"]["mappings"]["dynamic"].AsBoolean);
        }
        finally
        {
            await database.Client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, TestContext.Current.CancellationToken);
        }
    }

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
    public async Task Ensure_CreatesVectorIndexAndDoesNotRebuildItWhenUnchanged()
    {
        var books = fixture.Database.GetCollection<BookEntity>(BookEntity.CollectionName);
        var indexes = await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
        var vector = Assert.Single(indexes, index => index["name"] == "vector_index");
        Assert.Equal("vectorSearch", vector["type"].AsString);
        Assert.Equal("READY", vector["status"].AsString);
        var fields = vector["latestDefinition"]["fields"].AsBsonArray;
        Assert.Contains(fields, field => field["path"] == "Embedding" && field["numDimensions"] == 1536 && field["similarity"] == "cosine");
        Assert.Contains(fields, field => field["path"] == "UserId" && field["type"] == "filter");
        Assert.Contains(fields, field => field["path"] == "Status" && field["type"] == "filter");

        await MongoIndexSetup.EnsureAsync(fixture.Database, TestContext.Current.CancellationToken);
        var after = await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(vector["latestVersion"], Assert.Single(after, index => index["name"] == "vector_index")["latestVersion"]);
    }

    [Fact]
    public async Task VectorSearch_ReturnsNearestBookForUserAndStatus()
    {
        var books = fixture.Database.GetCollection<BsonDocument>(BookEntity.CollectionName);
        var userId = ObjectId.GenerateNewId();
        var otherUser = ObjectId.GenerateNewId();
        var near = new BsonArray(Enumerable.Range(0, 1536).Select(i => (BsonValue)(i == 0 ? 1.0 : 0.0)));
        var far = new BsonArray(Enumerable.Range(0, 1536).Select(i => (BsonValue)(i == 1 ? 1.0 : 0.0)));
        await books.InsertManyAsync([
            new BsonDocument { { "Title", "near" }, { "UserId", userId }, { "Status", 2 }, { "Embedding", near } },
            new BsonDocument { { "Title", "far" }, { "UserId", userId }, { "Status", 2 }, { "Embedding", far } },
            new BsonDocument { { "Title", "wrong status" }, { "UserId", userId }, { "Status", 0 }, { "Embedding", near } },
            new BsonDocument { { "Title", "wrong user" }, { "UserId", otherUser }, { "Status", 2 }, { "Embedding", near } }
        ], cancellationToken: TestContext.Current.CancellationToken);
        try
        {
            var stage = new BsonDocument("$vectorSearch", new BsonDocument
            {
                { "index", "vector_index" }, { "path", "Embedding" }, { "queryVector", near },
                { "numCandidates", 100 }, { "limit", 10 },
                { "filter", new BsonDocument("UserId", new BsonDocument("$in", new BsonArray { userId, otherUser })) }
            });
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            List<BsonDocument> result;
            do
            {
                result = await books.Aggregate<BsonDocument>(new[] { stage }, cancellationToken: timeout.Token).ToListAsync(timeout.Token);
                if (result.Count == 4) break;
                await Task.Delay(500, timeout.Token);
            } while (true);
            stage["$vectorSearch"]["filter"] = new BsonDocument { { "UserId", userId }, { "Status", 2 } };
            result = await books.Aggregate<BsonDocument>(new[] { stage }, cancellationToken: timeout.Token).ToListAsync(timeout.Token);
            Assert.Equal(new[] { "near", "far" }, result.Select(book => book["Title"].AsString));
        }
        finally
        {
            await books.DeleteManyAsync(new BsonDocument("UserId", new BsonDocument("$in", new BsonArray { userId, otherUser })), TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Ensure_UpdatesAnOutdatedVectorIndex()
    {
        var database = new MongoClient(fixture.ConnectionString).GetDatabase("vector-update-" + ObjectId.GenerateNewId());
        try
        {
            var books = database.GetCollection<BookEntity>(BookEntity.CollectionName);
            await books.Indexes.CreateOneAsync(new CreateIndexModel<BookEntity>(Builders<BookEntity>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: TestContext.Current.CancellationToken);
            var rawBooks = database.GetCollection<BsonDocument>(BookEntity.CollectionName);
            await rawBooks.InsertOneAsync(new BsonDocument("Title", "preserved book"), cancellationToken: TestContext.Current.CancellationToken);
            var oldDefinition = BsonDocument.Parse("{ fields: [{ type: 'vector', path: 'Embedding', numDimensions: 2, similarity: 'cosine' }] }");
            await books.SearchIndexes.CreateOneAsync(new CreateSearchIndexModel("vector_index", SearchIndexType.VectorSearch, oldDefinition), TestContext.Current.CancellationToken);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            while (true)
            {
                var indexes = await (await books.SearchIndexes.ListAsync(cancellationToken: timeout.Token)).ToListAsync(timeout.Token);
                if (indexes.Any(index => index["name"] == "vector_index" && index["status"] == "READY")) break;
                await Task.Delay(500, timeout.Token);
            }
            var updateError = await Record.ExceptionAsync(() => MongoIndexSetup.EnsureAsync(database, TestContext.Current.CancellationToken, updateExisting: true));
            if (updateError is not null)
            {
                Assert.IsType<MongoCommandException>(updateError);
                Assert.Contains("\"mappings\" is required", updateError.Message);
                var existing = await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
                Assert.Contains(existing, index => index["name"] == "vector_index");
                await MongoIndexSetup.EnsureAsync(database, TestContext.Current.CancellationToken, recreateVectorIndexOnUpdateFailure: true, updateExisting: true);
            }
            var result = await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
            var vector = Assert.Single(result, index => index["name"] == "vector_index");
            var field = Assert.Single(vector["latestDefinition"]["fields"].AsBsonArray, item => item["path"] == "Embedding");
            Assert.Equal(1536, field["numDimensions"].AsInt32);
            Assert.Equal("READY", vector["status"].AsString);
            Assert.Equal(1, await rawBooks.CountDocumentsAsync(new BsonDocument("Title", "preserved book"), cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            await database.Client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName, TestContext.Current.CancellationToken);
        }
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
            await Assert.ThrowsAsync<InvalidOperationException>(() => MongoIndexSetup.EnsureAsync(database, TestContext.Current.CancellationToken));
            var before = await (await books.SearchIndexes.ListAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
            Assert.True(Assert.Single(before, index => index["name"] == "default")["latestDefinition"]["mappings"]["dynamic"].AsBoolean);
            await MongoIndexSetup.EnsureAsync(database, TestContext.Current.CancellationToken, updateExisting: true);
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
