using System.Text.Json;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public class GoogleBooksMetadataService(HttpClient httpClient) : IBookMetadataService
{
    private const string GoogleBooksApiBaseUrl = "https://www.googleapis.com/books/v1/volumes";

    public async Task<TryResult<BookMetadata>> TryGetBookMetadataAsync(string isbn)
    {
        try
        {
            var response = await httpClient.GetAsync($"{GoogleBooksApiBaseUrl}?q=isbn:{isbn}");

            if (!response.IsSuccessStatusCode)
            {
                return new Error(
                    BookErrorCodes.MetadataServiceError,
                    $"Google Books API returned status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            if (!jsonDoc.RootElement.TryGetProperty("totalItems", out var totalItems) || totalItems.GetInt32() == 0)
            {
                return new Error(
                    BookErrorCodes.IsbnNotFound,
                    "No book found for the provided ISBN");
            }

            var items = jsonDoc.RootElement.GetProperty("items");
            var firstItem = items[0];
            var volumeInfo = firstItem.GetProperty("volumeInfo");

            var metadata = new BookMetadata
            {
                Title = volumeInfo.TryGetProperty("title", out var title)
                    ? title.GetString() ?? "Unknown Title"
                    : "Unknown Title",

                Authors = volumeInfo.TryGetProperty("authors", out var authors) && authors.ValueKind == JsonValueKind.Array
                    ? authors.EnumerateArray().Select(a => a.GetString()).Where(a => a != null).ToArray()
                    : null,

                Publisher = volumeInfo.TryGetProperty("publisher", out var publisher)
                    ? publisher.GetString()
                    : null,

                PublicationDate = volumeInfo.TryGetProperty("publishedDate", out var pubDate) &&
                                  DateTime.TryParse(pubDate.GetString(), out var parsedDate)
                    ? parsedDate
                    : null,

                Description = volumeInfo.TryGetProperty("description", out var desc)
                    ? desc.GetString()
                    : null,

                Pages = volumeInfo.TryGetProperty("pageCount", out var pageCount)
                    ? pageCount.GetInt32()
                    : null,

                CoverImageUrl = ExtractCoverImageUrl(volumeInfo),

                Isbn10 = ExtractIsbn(volumeInfo, "ISBN_10"),
                Isbn13 = ExtractIsbn(volumeInfo, "ISBN_13")
            };

            return metadata;
        }
        catch (HttpRequestException ex)
        {
            return new Error(
                BookErrorCodes.MetadataServiceError,
                $"Failed to connect to Google Books API: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new Error(
                BookErrorCodes.MetadataServiceError,
                $"Failed to parse Google Books API response: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new Error(
                BookErrorCodes.MetadataServiceError,
                $"Unexpected error retrieving book metadata: {ex.Message}");
        }
    }

    private static string? ExtractCoverImageUrl(JsonElement volumeInfo)
    {
        if (!volumeInfo.TryGetProperty("imageLinks", out var imageLinks))
            return null;

        // Prefer higher quality images
        if (imageLinks.TryGetProperty("large", out var large))
            return large.GetString();

        if (imageLinks.TryGetProperty("medium", out var medium))
            return medium.GetString();

        if (imageLinks.TryGetProperty("thumbnail", out var thumbnail))
            return thumbnail.GetString();

        return null;
    }

    private static string? ExtractIsbn(JsonElement volumeInfo, string isbnType)
    {
        if (!volumeInfo.TryGetProperty("industryIdentifiers", out var identifiers))
            return null;

        foreach (var identifier in identifiers.EnumerateArray())
        {
            if (identifier.TryGetProperty("type", out var type) &&
                type.GetString() == isbnType &&
                identifier.TryGetProperty("identifier", out var isbn))
            {
                return isbn.GetString();
            }
        }

        return null;
    }
}
