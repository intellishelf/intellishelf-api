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
}