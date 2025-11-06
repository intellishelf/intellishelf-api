using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public interface IBookService
{
    Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId);
    
    Task<TryResult<PagedResult<Book>>> TryGetPagedBooksAsync(string userId, BookQueryParameters queryParameters);

    Task<TryResult<Book>> TryGetBookAsync(string userId, string bookId);

    Task<TryResult<Book>> TryAddBookAsync(AddBookRequest request);

    Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request);

    Task<TryResult> TryDeleteBookAsync(string userId, string bookId);

    Task<TryResult<IReadOnlyCollection<Book>>> SearchAsync(string userId, string searchTerm);
    
    /// <summary>
    /// Adds a book to the user's collection by ISBN.
    /// Validates ISBN, checks for duplicates, fetches metadata from external APIs,
    /// downloads cover image, and persists to database.
    /// </summary>
    /// <param name="userId">Authenticated user ID</param>
    /// <param name="isbn">User-provided ISBN (10 or 13 digits, with or without hyphens)</param>
    /// <returns>
    /// Success: Created Book
    /// Failure: InvalidIsbn, DuplicateBook, BookNotFound, ExternalApiFailure errors
    /// </returns>
    Task<TryResult<Book>> TryAddBookByIsbnAsync(string userId, string isbn);
}