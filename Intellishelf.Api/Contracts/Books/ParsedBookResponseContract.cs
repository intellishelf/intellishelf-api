namespace Intellishelf.Api.Contracts.Books;

public class ParsedBookResponseContract
{
    public required string Language { get; init; }
    public required string Title { get; init; }
    public required string Authors { get; init; }
    public required string Publisher { get; init; }
    public required DateTime PublicationDate { get; init; }
    public required int Pages { get; init; }
    public required string Isbn { get; init; }
    public required string Description { get; init; }
}