using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Errors;

namespace Intellishelf.Domain.Books.Services;

/// <summary>
/// Service for looking up book metadata from Google Books API.
/// </summary>
public sealed class IsbnLookupService : IIsbnLookupService
{
    private readonly Google.Apis.Books.v1.BooksService _booksService;

    public IsbnLookupService(string apiKey)
    {
        _booksService = new Google.Apis.Books.v1.BooksService(
            new Google.Apis.Services.BaseClientService.Initializer
            {
                ApiKey = apiKey,
                ApplicationName = "Intellishelf"
            });
    }

    public async Task<TryResult<BookMetadata>> LookupByIsbnAsync(string isbn)
    {
        try
        {
            // Query Google Books API by ISBN
            var listRequest = _booksService.Volumes.List($"isbn:{isbn}");
            var volumes = await listRequest.ExecuteAsync();

            if (volumes.Items == null || volumes.Items.Count == 0)
            {
                return new Error(
                    BookErrorCodes.ExternalApiFailure,
                    $"No book found for ISBN: {isbn}");
            }

            // Take the first result
            var volumeInfo = volumes.Items[0].VolumeInfo;

            return new BookMetadata(
                Title: volumeInfo.Title ?? "Unknown Title",
                Authors: volumeInfo.Authors?.ToArray() ?? [],
                Publisher: volumeInfo.Publisher,
                PublicationDate: TryParsePublishedDate(volumeInfo.PublishedDate),
                Description: volumeInfo.Description,
                PageCount: volumeInfo.PageCount,
                CoverImageUrl: volumeInfo.ImageLinks?.Thumbnail?.Replace("http://", "https://"),
                Source: BookSource.Google
            );
        }
        catch (Exception ex)
        {
            return new Error(
                BookErrorCodes.ExternalApiFailure,
                $"Google Books API error: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to parse Google Books published date (format can vary: "YYYY", "YYYY-MM", "YYYY-MM-DD").
    /// </summary>
    private static DateTime? TryParsePublishedDate(string? publishedDate)
    {
        if (string.IsNullOrWhiteSpace(publishedDate))
            return null;

        // Try full date first
        if (DateTime.TryParse(publishedDate, out var fullDate))
            return fullDate;

        // Try year-only format
        if (int.TryParse(publishedDate, out var year) && year >= 1000 && year <= 9999)
            return new DateTime(year, 1, 1);

        return null;
    }
}
