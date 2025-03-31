namespace Intellishelf.Domain.Books.Models;

public class ParsedBook
{
    public required string? Title { get; init; }
    public required string? Author { get; init; }
    public required string? Publisher { get; init; }
    public required int? PublicationYear { get; init; }
    public required int? Pages { get; init; }
    public required string? Isbn { get; init; }
    public required string? Description { get; init; }
}