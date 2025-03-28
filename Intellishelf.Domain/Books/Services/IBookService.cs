using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public interface IBookService
{
    Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId);

    Task<TryResult> TryAddBookAsync(AddBook request);

    Task<TryResult> TryDeleteBookAsync(DeleteBook request);
}