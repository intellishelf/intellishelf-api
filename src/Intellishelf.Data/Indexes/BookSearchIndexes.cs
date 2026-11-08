using System.Text.Json;
using Intellishelf.Data.Books.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intellishelf.Data.Indexes;

public static class BookSearchIndexes
{
    public const string TextName = "default";
    public const string VectorName = "vector_index";
    public const int VectorDimensions = 1536;

    public static CreateSearchIndexModel Text()
    {
        static BsonDocument Type(string type) => new("type", type);
        static BsonArray TextAndAutocomplete() => new() { Type("string"), Type("autocomplete") };

        var fields = new BsonDocument
        {
            { nameof(BookEntity.Title), TextAndAutocomplete() },
            { nameof(BookEntity.Authors), TextAndAutocomplete() },
            { nameof(BookEntity.Tags), Type("string") },
            { nameof(BookEntity.Description), Type("string") },
            { nameof(BookEntity.Annotation), Type("string") },
            { nameof(BookEntity.Publisher), TextAndAutocomplete() },
            { nameof(BookEntity.UserId), Type("objectId") },
            { nameof(BookEntity.Status), Type("number") }
        };
        return new CreateSearchIndexModel(TextName, SearchIndexType.Search,
            new BsonDocument("mappings", new BsonDocument { { "dynamic", false }, { "fields", fields } }));
    }

    public static CreateSearchIndexModel Vector()
    {
        var fields = new BsonArray
        {
            new BsonDocument
            {
                { "type", "vector" }, { "path", nameof(BookEntity.Embedding) },
                { "numDimensions", VectorDimensions }, { "similarity", JsonNamingPolicy.CamelCase.ConvertName(VectorSimilarity.Cosine.ToString()) }
            },
            new BsonDocument { { "type", "filter" }, { "path", nameof(BookEntity.UserId) } },
            new BsonDocument { { "type", "filter" }, { "path", nameof(BookEntity.Status) } }
        };
        return new CreateSearchIndexModel(VectorName, SearchIndexType.VectorSearch, new BsonDocument("fields", fields));
    }
}
