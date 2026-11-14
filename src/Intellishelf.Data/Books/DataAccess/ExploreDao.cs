using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Books.Mappers;
using Intellishelf.Domain.Explore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intellishelf.Data.Books.DataAccess;

public sealed class ExploreDao(IMongoDatabase database, IBookEntityMapper mapper) : IExploreDao
{
    private readonly IMongoCollection<BsonDocument> _raw = database.GetCollection<BsonDocument>(BookEntity.CollectionName);
    private readonly IMongoCollection<BookEntity> _books = database.GetCollection<BookEntity>(BookEntity.CollectionName);

    public async Task<ExploreMatches> SearchAsync(string userId, string[] words, float[] vector, int bound, CancellationToken cancellationToken)
    {
        var owner = ObjectId.Parse(userId);
        async Task<IReadOnlyList<ExploreCandidate>> Keyword(bool titleOnly)
        {
            if (words.Length > 32) return [];
            var must = new BsonArray();
            foreach (var word in words)
            {
                var should = new BsonArray();
                foreach (var path in new[] { "Title", "Authors" })
                {
                    should.Add(new BsonDocument("text", new BsonDocument { { "query", word }, { "path", path } }));
                    should.Add(new BsonDocument("text", new BsonDocument { { "query", word }, { "path", path }, { "fuzzy", new BsonDocument { { "maxEdits", 1 }, { "prefixLength", 2 } } } }));
                }
                if (!titleOnly)
                    foreach (var path in new[] { "Tags", "Publisher", "Description", "Annotation" })
                        should.Add(new BsonDocument("text", new BsonDocument { { "query", word }, { "path", path } }));
                must.Add(new BsonDocument("compound", new BsonDocument { { "should", should }, { "minimumShouldMatch", 1 } }));
            }
            var search = new BsonDocument("$search", new BsonDocument("compound", new BsonDocument
            {
                { "filter", new BsonArray { new BsonDocument("equals", new BsonDocument { { "path", "UserId" }, { "value", owner } }) } },
                { "must", must }
            }));
            return await Run(search, "searchScore", bound + 1);
        }
        async Task<IReadOnlyList<ExploreCandidate>> Run(BsonDocument first, string score, int limit)
        {
            var pipeline = new[] { first, new BsonDocument("$project", new BsonDocument { { "_id", 1 }, { "score", new BsonDocument("$meta", score) } }), new BsonDocument("$sort", new BsonDocument { { "score", -1 }, { "_id", 1 } }), new BsonDocument("$limit", limit) };
            var rows = await _raw.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);
            return rows.Select(x => new ExploreCandidate(x["_id"].ToString()!, x["score"].ToDouble())).ToList();
        }
        var title = await Keyword(true);
        var keyword = await Keyword(false);
        var vectorSearch = new BsonDocument("$vectorSearch", new BsonDocument
        {
            { "index", "vector_index" }, { "path", "Embedding" }, { "queryVector", new BsonArray(vector.Select(v => (BsonValue)new BsonDouble(v))) },
            { "filter", new BsonDocument("UserId", owner) }, { "exact", true }, { "limit", bound }
        });
        var semantic = await Run(vectorSearch, "vectorSearchScore", bound);
        return new(title, keyword, semantic);
    }

    public async Task<IReadOnlyList<ExploreBook>> LoadAsync(string userId, IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var filter = Builders<BookEntity>.Filter.Eq(x => x.UserId, ObjectId.Parse(userId)) & Builders<BookEntity>.Filter.In(x => x.Id, ids);
        var projection = new BsonDocumentProjectionDefinition<BookEntity, BookEntity>(new BsonDocument { { "Embedding", 0 }, { "EmbeddingHash", 0 }, { "EmbeddingModel", 0 } });
        var books = await _books.Find(filter).Project(projection).ToListAsync(cancellationToken);
        return books.Select(x => new ExploreBook(mapper.Map(x), x.Authors ?? [])).ToList();
    }
}
