namespace Intellishelf.Domain.Books.Models;

public class BookMetadata
{
    public required string Title { get; init; }
    public string? Authors { get; init; }
    public string? Publisher { get; init; }
    public DateTime? PublicationDate { get; init; }
    public string? Description { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    public int? Pages { get; init; }
    public string? CoverImageUrl { get; init; }
}
