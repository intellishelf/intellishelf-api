namespace Intellishelf.Domain.Books;

/// <summary>
/// Metadata retrieved from external APIs (Google Books or Amazon).
/// Intermediate DTO before converting to BookEntity.
/// </summary>
public sealed record BookMetadata(
    string Title,
    string[] Authors,
    string? Publisher,
    DateTime? PublicationDate,
    string? Description,
    int? PageCount,
    string? CoverImageUrl,
    BookSource Source
);
