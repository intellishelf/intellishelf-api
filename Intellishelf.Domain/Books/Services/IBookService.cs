using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public interface IBookService
{
    Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId);

    Task<TryResult<Book>> TryGetBookAsync(string userId, string bookId);

    Task<TryResult> TryAddBookAsync(AddBookRequest request);

    Task<TryResult> TryDeleteBookAsync(DeleteBookRequest request);
}