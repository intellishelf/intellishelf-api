namespace Intellishelf.Domain.Chat.Models;

public class BookChatContext
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Authors { get; init; }
    public string? Publisher { get; init; }
    public DateTime? PublicationDate { get; init; }
    public int? Pages { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    public required string Status { get; init; }
    public DateTime? StartedReadingDate { get; init; }
    public DateTime? FinishedReadingDate { get; init; }
    public string[]? Tags { get; init; }
}
