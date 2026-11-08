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
        var book = BsonClassMap.LookupClassMap(typeof(BookEntity));
        string Field(string name) => book.GetMemberMap(name).ElementName;
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

    public static CreateVectorSearchIndexModel<BookEntity> Vector() => new(
        book => book.Embedding!, VectorName, VectorSimilarity.Cosine, VectorDimensions,
        book => book.UserId, book => book.Status);

    // The driver renders the typed model internally when creating an index, but exposes
    // no equivalent public renderer for comparing or updating an existing definition.
    public static BsonDocument VectorDefinition(CreateVectorSearchIndexModel<BookEntity> model)
    {
        var args = new RenderArgs<BookEntity>(BsonSerializer.LookupSerializer<BookEntity>(), BsonSerializer.SerializerRegistry);
        var fields = new BsonArray
        {
            new BsonDocument
            {
                { "type", "vector" }, { "path", model.Field.Render(args).FieldName },
                { "numDimensions", model.Dimensions }, { "similarity", JsonNamingPolicy.CamelCase.ConvertName(model.Similarity.ToString()) }
            }
        };
        foreach (var field in model.FilterFields)
            fields.Add(new BsonDocument { { "type", "filter" }, { "path", field.Render(args).FieldName } });
        return new BsonDocument("fields", fields);
    }
}
