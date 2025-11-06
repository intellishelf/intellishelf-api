using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Files.Services;

namespace Intellishelf.Domain.Books.Services;

public class BookService(
    IBookDao bookDao,
    IFileStorageService fileStorageService,
    IIsbnLookupService isbnLookupService) : IBookService
{
    public async Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId) =>
        await bookDao.GetBooksAsync(userId);

    public async Task<TryResult<PagedResult<Book>>> TryGetPagedBooksAsync(string userId, BookQueryParameters queryParameters) =>
        await bookDao.GetPagedBooksAsync(userId, queryParameters);

    public async Task<TryResult<Book>> TryGetBookAsync(string userId, string bookId) =>
        await bookDao.GetBookAsync(userId, bookId);

    public async Task<TryResult<Book>> TryAddBookAsync(AddBookRequest request) =>
        await bookDao.AddBookAsync(request);

    public async Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request)
    {
        var existingBook = await TryGetBookAsync(request.UserId, request.Id);

        if (request.CoverImageUrl != null && existingBook.IsSuccess &&
            !string.IsNullOrEmpty(existingBook.Value.CoverImageUrl))
        {
            await fileStorageService.DeleteFileFromUrlAsync(existingBook.Value.CoverImageUrl);
        }

        return await bookDao.TryUpdateBookAsync(request);
    }

    public async Task<TryResult> TryDeleteBookAsync(string userId, string bookId)
    {
        var existingBook = await TryGetBookAsync(userId, bookId);

        if (existingBook.IsSuccess && !string.IsNullOrEmpty(existingBook.Value.CoverImageUrl))
        {
            await fileStorageService.DeleteFileFromUrlAsync(existingBook.Value.CoverImageUrl);
        }

        return await bookDao.DeleteBookAsync(userId, bookId);
    }

    public async Task<TryResult<IReadOnlyCollection<Book>>> SearchAsync(string userId, string searchTerm)
    {
       return await bookDao.SearchAsync(userId, searchTerm);
    }
    
    public async Task<TryResult<Book>> TryAddBookByIsbnAsync(string userId, string isbn)
    {
        // 1. Normalize ISBN (remove hyphens and spaces)
        var normalizedIsbn = NormalizeIsbn(isbn);

        // 2. Validate ISBN format
        if (!IsValidIsbnFormat(normalizedIsbn, out var isbn10, out var isbn13))
        {
            return new Error(
                BookErrorCodes.InvalidIsbn,
                $"Invalid ISBN format: '{isbn}'. Must be 10 or 13 digits.");
        }

        // 3. Check for duplicates
        var existingBook = await bookDao.FindByIsbnAsync(userId, isbn10, isbn13);
        if (existingBook != null)
        {
            return new Error(
                BookErrorCodes.DuplicateBook,
                $"Book with ISBN {normalizedIsbn} already exists in your collection.");
        }

        // 4. Lookup metadata from external API
        var metadataResult = await isbnLookupService.LookupByIsbnAsync(normalizedIsbn);
        if (!metadataResult.IsSuccess)
        {
            return metadataResult.Error; // Propagate external API failure
        }

        var metadata = metadataResult.Value;

        // 5. Download cover image if available
        string? coverImageUrl = null;
        if (!string.IsNullOrWhiteSpace(metadata.CoverImageUrl))
        {
            try
            {
                using var httpClient = new HttpClient();
                await using var imageStream = await httpClient.GetStreamAsync(metadata.CoverImageUrl);
                var fileName = $"isbn-{normalizedIsbn}.jpg";

                var uploadResult = await fileStorageService.UploadFileAsync(userId, imageStream, fileName);
                if (uploadResult.IsSuccess)
                {
                    coverImageUrl = uploadResult.Value;
                }
                // Ignore upload failures - book can exist without cover
            }
            catch
            {
                // Ignore download failures - book can exist without cover
            }
        }

        // 6. Create book request and persist
        var addRequest = new AddBookRequest
        {
            UserId = userId,
            Title = metadata.Title,
            Authors = metadata.Authors.Length > 0 ? metadata.Authors : null,
            Publisher = metadata.Publisher,
            PublicationDate = metadata.PublicationDate,
            Description = metadata.Description,
            Pages = metadata.PageCount,
            Isbn10 = isbn10,
            Isbn13 = isbn13,
            CoverImageUrl = coverImageUrl
        };

        return await bookDao.AddBookAsync(addRequest);
    }

    /// <summary>
    /// Normalizes an ISBN by removing hyphens and spaces.
    /// </summary>
    private static string NormalizeIsbn(string isbn) =>
        isbn.Replace("-", "").Replace(" ", "").Trim();

    /// <summary>
    /// Validates ISBN format and extracts ISBN-10 or ISBN-13.
    /// </summary>
    /// <returns>True if valid ISBN-10 or ISBN-13; false otherwise.</returns>
    private static bool IsValidIsbnFormat(string normalizedIsbn, out string? isbn10, out string? isbn13)
    {
        isbn10 = null;
        isbn13 = null;

        if (normalizedIsbn.Length == 10 && normalizedIsbn.All(char.IsDigit))
        {
            // Valid ISBN-10 format (basic check - not validating checksum)
            isbn10 = normalizedIsbn;
            return true;
        }

        if (normalizedIsbn.Length == 13 && normalizedIsbn.All(char.IsDigit))
        {
            // Valid ISBN-13 format (basic check - not validating checksum)
            // Must start with 978 or 979 per spec
            if (normalizedIsbn.StartsWith("978") || normalizedIsbn.StartsWith("979"))
            {
                isbn13 = normalizedIsbn;
                return true;
            }
        }

        return false;
    }
}