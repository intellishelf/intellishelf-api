using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.DataAccess;

public interface IBookDao
{
    Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId);

    Task<TryResult<PagedResult<Book>>> GetPagedBooksAsync(string userId, BookQueryParameters queryParameters);

    Task<TryResult<Book>> GetBookAsync(string userId, string bookId);

    Task<TryResult<Book?>> FindByIsbnAsync(string userId, string? isbn10, string? isbn13);

    Task<TryResult<Book>> AddBookAsync(AddBookRequest request);

    Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request);

    Task<TryResult> DeleteBookAsync(string userId, string bookId);

    Task<TryResult<PagedResult<Book>>> SearchAsync(string userId, SearchQueryParameters queryParameters);

    Task<TryResult<long>> DeleteAllBooksByUserAsync(string userId);
}