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
        var vectorPath = model.Field.Render(args).FieldName;
        var vector = new BsonDocument
        {
            { "type", "vector" }, { "path", vectorPath },
            { "numDimensions", model.Dimensions }, { "similarity", JsonNamingPolicy.CamelCase.ConvertName(model.Similarity.ToString()) }
        };
        if (model.IndexingMethod == VectorIndexingMethod.Flat && (model.HnswMaxEdges.HasValue || model.HnswNumEdgeCandidates.HasValue))
            throw new InvalidOperationException("HNSW options cannot be used with flat indexing.");
        if (model.Quantization is not null and not VectorQuantization.None)
            vector.Add("quantization", JsonNamingPolicy.CamelCase.ConvertName(model.Quantization.ToString()!));
        if (model.HnswMaxEdges.HasValue || model.HnswNumEdgeCandidates.HasValue)
            vector.Add("hnswOptions", new BsonDocument { { "maxEdges", model.HnswMaxEdges ?? 16 }, { "numEdgeCandidates", model.HnswNumEdgeCandidates ?? 100 } });
        if (model.IndexingMethod.HasValue)
            vector.Add("indexingMethod", JsonNamingPolicy.CamelCase.ConvertName(model.IndexingMethod.ToString()!));

        var fields = new BsonArray { vector };
        foreach (var field in model.FilterFields)
            fields.Add(new BsonDocument { { "type", "filter" }, { "path", field.Render(args).FieldName } });
        var definition = new BsonDocument("fields", fields);
        var dot = vectorPath.LastIndexOf('.');
        if (dot > 0)
            definition.Add("nestedRoot", vectorPath[..dot]);
        var excluded = model.ExcludedStoredFields?.Count > 0;
        var storedFields = excluded ? model.ExcludedStoredFields : model.IncludedStoredFields;
        if (storedFields?.Count > 0)
            definition.Add("storedSource", new BsonDocument(excluded ? "exclude" : "include", new BsonArray(storedFields.Select(field => field.Render(args).FieldName))));
        return definition;
    }
}
