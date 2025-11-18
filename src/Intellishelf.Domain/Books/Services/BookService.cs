using Intellishelf.Domain.Ai.Services;
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
    IHttpImageDownloader httpImageDownloader,
    IAiService aiService) : IBookService
{
    public async Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId) =>
        await bookDao.GetBooksAsync(userId);

    public async Task<TryResult<PagedResult<Book>>> TryGetPagedBooksAsync(string userId, BookQueryParameters queryParameters) =>
        await bookDao.GetPagedBooksAsync(userId, queryParameters);

    public async Task<TryResult<Book>> TryGetBookAsync(string userId, string bookId) =>
        await bookDao.GetBookAsync(userId, bookId);

    public async Task<TryResult<Book>> TryAddBookAsync(AddBookRequest request)
    {
        var addResult = await bookDao.AddBookAsync(request);
        if (!addResult.IsSuccess)
            return addResult;

        var book = addResult.Value;

        // Generate and store embedding
        var embeddingResult = await aiService.GenerateEmbeddingAsync(book);
        if (embeddingResult.IsSuccess)
        {
            await bookDao.UpdateEmbeddingAsync(book.UserId, book.Id, embeddingResult.Value);
        }
        // If embedding generation fails, continue anyway - the book is already added

        return book;
    }

    public async Task<TryResult<Book>> TryAddBookFromIsbnAsync(string userId, string isbn)
    {
        // Validate ISBN format
        if (!IsbnHelper.IsValidIsbn(isbn))
        {
            return new Error(BookErrorCodes.InvalidIsbn, "The provided ISBN format is invalid");
        }

        // Normalize ISBN
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(isbn);

        // Fetch metadata from Google Books
        var metadataResult = await bookMetadataService.TryGetBookMetadataAsync(normalizedIsbn);
        if (!metadataResult.IsSuccess)
            return new Error(metadataResult.Error!.Code, metadataResult.Error.Message);

        var metadata = metadataResult.Value;

        // Check for duplicate using ISBNs from Google Books response
        var existingBookResult = await bookDao.FindByIsbnAsync(userId, metadata.Isbn10, metadata.Isbn13);
        if (!existingBookResult.IsSuccess)
            return new Error(existingBookResult.Error!.Code, existingBookResult.Error.Message);

        if (existingBookResult.Value != null)
        {
            return new Error(
                BookErrorCodes.DuplicateIsbn,
                "You already have this book in your library");
        }

        // Download and upload cover image if available
        string? coverImageUrl = null;
        if (!string.IsNullOrWhiteSpace(metadata.CoverImageUrl))
        {
            var imageStreamResult = await httpImageDownloader.DownloadImageAsync(metadata.CoverImageUrl);
            if (imageStreamResult.IsSuccess)
            {
                var fileName = $"{Guid.NewGuid()}.jpg";
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
            Isbn10 = metadata.Isbn10,
            Isbn13 = metadata.Isbn13,
            Pages = metadata.Pages,
            CoverImageUrl = coverImageUrl,
            Status = ReadingStatus.Unread,
            Tags = null,
            Annotation = null,
            StartedReadingDate = null,
            FinishedReadingDate = null
        };

        var addResult = await bookDao.AddBookAsync(addBookRequest);
        if (!addResult.IsSuccess)
            return addResult;

        var book = addResult.Value;

        // Generate and store embedding
        var embeddingResult = await aiService.GenerateEmbeddingAsync(book);
        if (embeddingResult.IsSuccess)
        {
            await bookDao.UpdateEmbeddingAsync(book.UserId, book.Id, embeddingResult.Value);
        }
        // If embedding generation fails, continue anyway - the book is already added

        return book;
    }

    public async Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request)
    {
        var existingBook = await TryGetBookAsync(request.UserId, request.Id);

        if (request.CoverImageUrl != null && existingBook.IsSuccess &&
            !string.IsNullOrEmpty(existingBook.Value.CoverImageUrl))
        {
            await fileStorageService.DeleteFileFromUrlAsync(existingBook.Value.CoverImageUrl);
        }

        var updateResult = await bookDao.TryUpdateBookAsync(request);
        if (!updateResult.IsSuccess)
            return updateResult;

        // Get the updated book and regenerate embedding
        var updatedBookResult = await bookDao.GetBookAsync(request.UserId, request.Id);
        if (updatedBookResult.IsSuccess)
        {
            var embeddingResult = await aiService.GenerateEmbeddingAsync(updatedBookResult.Value);
            if (embeddingResult.IsSuccess)
            {
                await bookDao.UpdateEmbeddingAsync(request.UserId, request.Id, embeddingResult.Value);
            }
            // If embedding generation fails, continue anyway - the book is already updated
        }

        return TryResult.Success();
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
        // If no embedding is provided, generate one from the search term for semantic search
        if (queryParameters.SearchEmbedding == null || queryParameters.SearchEmbedding.Length == 0)
        {
            // Create a temporary book object to generate embedding from search term
            var tempBook = new Book
            {
                Id = string.Empty,
                UserId = userId,
                Title = queryParameters.SearchTerm,
                CreatedDate = DateTime.UtcNow,
                Status = ReadingStatus.Unread,
                Authors = null,
                Description = null,
                Tags = null
            };

            var embeddingResult = await aiService.GenerateEmbeddingAsync(tempBook);
            if (embeddingResult.IsSuccess)
            {
                // Create new query parameters with the embedding
                queryParameters = new SearchQueryParameters
                {
                    SearchTerm = queryParameters.SearchTerm,
                    Page = queryParameters.Page,
                    PageSize = queryParameters.PageSize,
                    Status = queryParameters.Status,
                    SearchEmbedding = embeddingResult.Value
                };
            }
            // If embedding generation fails, continue with text-only search
        }

        return await bookDao.SearchAsync(userId, queryParameters);
    }
}