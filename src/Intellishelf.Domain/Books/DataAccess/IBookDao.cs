using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.DataAccess;

public interface IBookDao
{
    Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId);
    
    Task<TryResult<PagedResult<Book>>> GetPagedBooksAsync(string userId, BookQueryParameters queryParameters);

    Task<TryResult<Book>> GetBookAsync(string userId, string bookId);

    Task<TryResult<Book>> AddBookAsync(AddBookRequest request);

    Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request);

    Task<TryResult> DeleteBookAsync(string userId, string bookId);

    Task<TryResult<IReadOnlyCollection<Book>>> SearchAsync(string userId, string searchTerm);
    
    /// <summary>
    /// Finds a book by ISBN-10 or ISBN-13 for a specific user.
    /// Used for duplicate detection before adding new book.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="isbn10">Normalized ISBN-10 (nullable)</param>
    /// <param name="isbn13">Normalized ISBN-13 (nullable)</param>
    /// <returns>Existing Book if found, null otherwise</returns>
    Task<Book?> FindByIsbnAsync(string userId, string? isbn10, string? isbn13);

    /// <summary>
    /// Ensures MongoDB indexes are created for books collection.
    /// Should be called at application startup.
    /// </summary>
    Task EnsureIndexesAsync();
}