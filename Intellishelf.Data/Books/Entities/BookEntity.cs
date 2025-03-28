using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intellishelf.Data.Books.Entities;

public class BookEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; init; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public required string UserId { get; init; }

    public required string Title { get; init; }
    public string[]? Authors { get; init; }
    public DateTime? PublicationDate { get; init; }
    public string? Isbn { get; init; }
    public string? Publisher { get; init; }
    public int? Pages { get; init; }
    public string? ImageUrl { get; init; }
    public string? Annotation { get; init; }
    public string? Description { get; init; }
    public string[]? Tags { get; init; }
    public required DateTime CreatedDate { get; init; }
}