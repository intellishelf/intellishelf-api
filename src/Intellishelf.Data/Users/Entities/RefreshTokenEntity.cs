using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intellishelf.Data.Users.Entities;

public class RefreshTokenEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; init; } = null!;
    
    public required string Token { get; init; }
    public required string UserId { get; init; }
    public required DateTime ExpiryDate { get; init; }
    public required bool IsRevoked { get; init; }
    public required DateTime CreatedAt { get; init; }
}
