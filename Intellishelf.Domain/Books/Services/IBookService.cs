using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public interface IBookService
{
    Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId);

    Task<TryResult> TryAddBookAsync(AddBookRequest request);

    Task<TryResult> TryDeleteBookAsync(DeleteBookRequest request);
}