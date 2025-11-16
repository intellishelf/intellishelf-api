using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Api.Contracts.Books;

public class BookRequestContractBase
{
    public required string Title { get; init; }

    public string? Annotation { get; init; }
    public string[]? Authors { get; init; }
    public string? Description { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    public int? Pages { get; init; }
    public DateTime? PublicationDate { get; init; }
    public string? Publisher { get; init; }
    public string[]? Tags { get; init; }
    public IFormFile? ImageFile { get; init; }

    public ReadingStatus Status { get; init; } = ReadingStatus.Unread;
    public DateTime? StartedReadingDate { get; init; }
    public DateTime? FinishedReadingDate { get; init; }
}