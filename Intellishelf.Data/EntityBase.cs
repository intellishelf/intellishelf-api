using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intellishelf.Data;

public abstract class EntityBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; init; } = null!;
}