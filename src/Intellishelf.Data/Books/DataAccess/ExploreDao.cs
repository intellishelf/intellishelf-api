using Intellishelf.Data.Books.Entities;
using Intellishelf.Common.TryResult;
using Intellishelf.Data.Books.Mappers;
using Intellishelf.Domain.Explore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intellishelf.Data.Books.DataAccess;

public sealed class ExploreDao(IMongoDatabase database, IBookEntityMapper mapper) : IExploreDao
{
    private readonly IMongoCollection<BsonDocument> _raw = database.GetCollection<BsonDocument>(BookEntity.CollectionName);
    private readonly IMongoCollection<BookEntity> _books = database.GetCollection<BookEntity>(BookEntity.CollectionName);

    public async Task<TryResult<ExploreMatches>> TrySearchAsync(string userId, string[] words, float[] vector, int bound, CancellationToken cancellationToken)
    {
        var owner = ObjectId.Parse(userId);
        async Task<IReadOnlyList<ExploreCandidate>> Keyword(bool titleOnly)
        {
            BsonDocument SearchStage(IEnumerable<string> batch)
            {
                var must = new BsonArray();
                foreach (var word in batch)
                {
                    var titlePaths = new BsonArray { "Title", "Authors" };
                    var allPaths = new BsonArray { "Title", "Authors", "Tags", "Publisher", "Description", "Annotation" };
                    var should = new BsonArray
                    {
                        new BsonDocument("text", new BsonDocument { { "query", word }, { "path", titleOnly ? titlePaths : allPaths } }),
                        new BsonDocument("text", new BsonDocument { { "query", word }, { "path", titlePaths }, { "fuzzy", new BsonDocument { { "maxEdits", 1 }, { "prefixLength", 0 } } } })
                    };
                    must.Add(new BsonDocument("compound", new BsonDocument { { "should", should }, { "minimumShouldMatch", 1 } }));
                }
                return new BsonDocument("$search", new BsonDocument("compound", new BsonDocument
                {
                    { "filter", new BsonArray { new BsonDocument("equals", new BsonDocument { { "path", "UserId" }, { "value", owner } }) } },
                    { "must", must }
                }));
            }
            var batches = words.Chunk(4).ToArray();
            if (batches.Length == 0) return Array.Empty<ExploreCandidate>();
            if (batches.Length == 1) return await Run(SearchStage(batches[0]), "searchScore", bound + 1);
            var project = new BsonDocument("$project", new BsonDocument { { "_id", 1 }, { "score", new BsonDocument("$meta", "searchScore") } });
            var pipeline = new List<BsonDocument> { SearchStage(batches[0]), project };
            foreach (var batch in batches.Skip(1))
                pipeline.Add(new BsonDocument("$unionWith", new BsonDocument { { "coll", BookEntity.CollectionName }, { "pipeline", new BsonArray { SearchStage(batch), project } } }));
            pipeline.Add(new BsonDocument("$group", new BsonDocument { { "_id", "$_id" }, { "matches", new BsonDocument("$sum", 1) }, { "score", new BsonDocument("$sum", "$score") } }));
            pipeline.Add(new BsonDocument("$match", new BsonDocument("matches", batches.Length)));
            pipeline.Add(new BsonDocument("$sort", new BsonDocument { { "score", -1 }, { "_id", 1 } }));
            pipeline.Add(new BsonDocument("$limit", bound + 1));
            var rows = await _raw.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);
            return rows.Select(x => new ExploreCandidate(x["_id"].ToString()!, x["score"].ToDouble())).ToList();
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
            { "filter", new BsonDocument("UserId", owner) }, { "exact", true }, { "limit", bound + 1 }
        });
        var semantic = await Run(vectorSearch, "vectorSearchScore", bound + 1);
        return new ExploreMatches(title, keyword, semantic);
    }

    public async Task<TryResult<IReadOnlyList<ExploreBook>>> TryLoadAsync(string userId, IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var filter = Builders<BookEntity>.Filter.Eq(x => x.UserId, ObjectId.Parse(userId)) & Builders<BookEntity>.Filter.In(x => x.Id, ids);
        var projection = new BsonDocumentProjectionDefinition<BookEntity, BookEntity>(new BsonDocument { { "Embedding", 0 }, { "EmbeddingHash", 0 }, { "EmbeddingModel", 0 } });
        var books = await _books.Find(filter).Project(projection).ToListAsync(cancellationToken);
        return TryResult.Success<IReadOnlyList<ExploreBook>>(books.Select(x => new ExploreBook(mapper.Map(x), x.Authors ?? [])).ToList());
    }
}
