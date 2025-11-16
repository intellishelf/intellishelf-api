using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Books.Helpers;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Files.Services;

namespace Intellishelf.Domain.Books.Services;

public class BookService(
    IBookDao bookDao,
    IFileStorageService fileStorageService,
    IBookMetadataService bookMetadataService,
    IHttpImageDownloader httpImageDownloader) : IBookService
{
    public async Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId) =>
        await bookDao.GetBooksAsync(userId);

    public async Task<TryResult<PagedResult<Book>>> TryGetPagedBooksAsync(string userId, BookQueryParameters queryParameters) =>
        await bookDao.GetPagedBooksAsync(userId, queryParameters);

    public async Task<TryResult<Book>> TryGetBookAsync(string userId, string bookId) =>
        await bookDao.GetBookAsync(userId, bookId);

    public async Task<TryResult<Book>> TryAddBookAsync(AddBookRequest request) =>
        await bookDao.AddBookAsync(request);

    public async Task<TryResult<Book>> TryAddBookFromIsbnAsync(string userId, string isbn)
    {
        // Validate ISBN format
        if (!IsbnHelper.IsValidIsbn(isbn))
        {
            return new Error(BookErrorCodes.InvalidIsbn, "The provided ISBN format is invalid");
        }

        // Normalize and determine ISBN type
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(isbn);
        string? isbn10 = null;
        string? isbn13 = null;

        if (normalizedIsbn.Length == 10)
        {
            isbn10 = normalizedIsbn;
            isbn13 = IsbnHelper.ConvertIsbn10ToIsbn13(normalizedIsbn);
        }
        else if (normalizedIsbn.Length == 13)
        {
            isbn13 = normalizedIsbn;
        }

        // Check for duplicate
        var existingBookResult = await bookDao.FindByIsbnAsync(userId, isbn10, isbn13);
        if (!existingBookResult.IsSuccess)
            return new Error(existingBookResult.Error!.Code, existingBookResult.Error.Message);

        if (existingBookResult.Value != null)
        {
            return new Error(
                BookErrorCodes.DuplicateIsbn,
                "You already have this book in your library");
        }

        // Fetch metadata from Google Books
        var metadataResult = await bookMetadataService.TryGetBookMetadataAsync(normalizedIsbn);
        if (!metadataResult.IsSuccess)
            return new Error(metadataResult.Error!.Code, metadataResult.Error.Message);

        var metadata = metadataResult.Value;

        // Use ISBNs from metadata if available, otherwise use our normalized values
        isbn10 = metadata.Isbn10 ?? isbn10;
        isbn13 = metadata.Isbn13 ?? isbn13;

        // Download and upload cover image if available
        string? coverImageUrl = null;
        if (!string.IsNullOrWhiteSpace(metadata.CoverImageUrl))
        {
            var imageStreamResult = await httpImageDownloader.DownloadImageAsync(metadata.CoverImageUrl);
            if (imageStreamResult.IsSuccess)
            {
                var fileName = $"{isbn13 ?? isbn10}.jpg";
                var uploadResult = await fileStorageService.UploadFileAsync(userId, imageStreamResult.Value, fileName);

                if (uploadResult.IsSuccess)
                {
                    coverImageUrl = uploadResult.Value;
                }
                // If upload fails, just continue without cover image
            }
            // If download fails, just continue without cover image
        }

        // Create book from metadata
        var addBookRequest = new AddBookRequest
        {
            UserId = userId,
            Title = metadata.Title,
            Authors = metadata.Authors,
            Publisher = metadata.Publisher,
            PublicationDate = metadata.PublicationDate,
            Description = metadata.Description,
            Isbn10 = isbn10,
            Isbn13 = isbn13,
            Pages = metadata.Pages,
            CoverImageUrl = coverImageUrl,
            Status = ReadingStatus.Unread,
            Tags = null,
            Annotation = null,
            StartedReadingDate = null,
            FinishedReadingDate = null
        };

        return await bookDao.AddBookAsync(addBookRequest);
    }

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

    public async Task<TryResult<PagedResult<Book>>> SearchAsync(string userId, SearchQueryParameters queryParameters)
    {
       return await bookDao.SearchAsync(userId, queryParameters);
    }
}