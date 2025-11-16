using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Users.Entities;
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
            .WithImage("mongodb/mongodb-atlas-local:latest")
            .Build();

        await _mongoContainer.StartAsync();

        var client = new MongoClient(_mongoContainer.GetConnectionString());
        Database = client.GetDatabase("intellishelf-test");

        // Create Atlas Search index for books collection
        await CreateSearchIndexAsync();
    }

    private async Task CreateSearchIndexAsync()
    {
        var booksCollection = Database.GetCollection<BookEntity>(BookEntity.CollectionName);

        // Define the Atlas Search index with autocomplete and text fields
        var searchIndexDefinition = new MongoDB.Bson.BsonDocument
        {
            { "mappings", new MongoDB.Bson.BsonDocument
                {
                    { "dynamic", false },
                    { "fields", new MongoDB.Bson.BsonDocument
                        {
                            { "Title", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "string" },
                                    { "analyzer", "lucene.standard" }
                                }
                            },
                            { "Title", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "autocomplete" },
                                    { "analyzer", "lucene.standard" },
                                    { "tokenization", "edgeGram" },
                                    { "minGrams", 2 },
                                    { "maxGrams", 15 },
                                    { "foldDiacritics", true }
                                }
                            },
                            { "Authors", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "string" },
                                    { "analyzer", "lucene.standard" }
                                }
                            },
                            { "Authors", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "autocomplete" },
                                    { "analyzer", "lucene.standard" },
                                    { "tokenization", "edgeGram" },
                                    { "minGrams", 2 },
                                    { "maxGrams", 15 },
                                    { "foldDiacritics", true }
                                }
                            },
                            { "Publisher", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "string" },
                                    { "analyzer", "lucene.standard" }
                                }
                            },
                            { "Publisher", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "autocomplete" },
                                    { "analyzer", "lucene.standard" },
                                    { "tokenization", "edgeGram" },
                                    { "minGrams", 2 },
                                    { "maxGrams", 15 },
                                    { "foldDiacritics", true }
                                }
                            },
                            { "Tags", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "string" },
                                    { "analyzer", "lucene.standard" }
                                }
                            },
                            { "Description", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "string" },
                                    { "analyzer", "lucene.standard" }
                                }
                            },
                            { "Annotation", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "string" },
                                    { "analyzer", "lucene.standard" }
                                }
                            },
                            { "UserId", new MongoDB.Bson.BsonDocument
                                {
                                    { "type", "objectId" }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Create the search index
        var indexName = await booksCollection.SearchIndexes.CreateOneAsync(
            new MongoDB.Driver.CreateSearchIndexModel("default", searchIndexDefinition));

        // Poll until the index is ready (usually takes 2-10 seconds)
        var maxWaitTime = TimeSpan.FromSeconds(30);
        var pollInterval = TimeSpan.FromMilliseconds(500);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            var indexes = await booksCollection.SearchIndexes.ListAsync().ToListAsync();
            var index = indexes.FirstOrDefault(i => i["name"] == "default");

            if (index != null && index.Contains("status") && index["status"] == "READY")
            {
                return; // Index is ready
            }

            await Task.Delay(pollInterval);
        }

        throw new TimeoutException("Search index did not become ready within the expected time.");
    }

    public async Task DisposeAsync() =>
        await _mongoContainer.DisposeAsync();

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