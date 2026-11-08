using System.Text.Json;
using Intellishelf.Data.Books.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
            { Field(nameof(BookEntity.Title)), TextAndAutocomplete() },
            { Field(nameof(BookEntity.Authors)), TextAndAutocomplete() },
            { Field(nameof(BookEntity.Tags)), Type("string") },
            { Field(nameof(BookEntity.Description)), Type("string") },
            { Field(nameof(BookEntity.Annotation)), Type("string") },
            { Field(nameof(BookEntity.Publisher)), TextAndAutocomplete() },
            { Field(nameof(BookEntity.UserId)), Type("objectId") },
            { Field(nameof(BookEntity.Status)), Type("number") }
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
                { "type", "vector" }, { "path", Field(nameof(BookEntity.Embedding)) },
                { "numDimensions", VectorDimensions }, { "similarity", JsonNamingPolicy.CamelCase.ConvertName(VectorSimilarity.Cosine.ToString()) }
            },
            new BsonDocument { { "type", "filter" }, { "path", Field(nameof(BookEntity.UserId)) } },
            new BsonDocument { { "type", "filter" }, { "path", Field(nameof(BookEntity.Status)) } }
        };
        return new CreateSearchIndexModel(VectorName, SearchIndexType.VectorSearch, new BsonDocument("fields", fields));
    }

    private static string Field(string name) =>
        BsonClassMap.LookupClassMap(typeof(BookEntity)).GetMemberMap(name).ElementName;

}
