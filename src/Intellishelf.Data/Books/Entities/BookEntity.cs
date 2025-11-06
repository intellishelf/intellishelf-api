using Intellishelf.Domain.Books;
using MongoDB.Bson;

namespace Intellishelf.Data.Books.Entities;

public class BookEntity : EntityBase
{
    public const string CollectionName = "Books";

    public required ObjectId UserId { get; init; }

    public required string Title { get; init; }
    public string[]? Authors { get; init; }
    public DateTime? PublicationDate { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    public string? Publisher { get; init; }
    public int? Pages { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? Annotation { get; init; }
    public string? Description { get; init; }
    public string[]? Tags { get; init; }
    
    /// <summary>
    /// External API source for book metadata.
    /// Used for analytics and debugging.
    /// </summary>
    public BookSource? Source { get; init; }
    
    public required DateTime CreatedDate { get; init; }
    public required DateTime ModifiedDate { get; init; }
}