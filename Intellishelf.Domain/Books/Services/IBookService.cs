using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public interface IBookService
{
    Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId);

    Task<TryResult> AddBookAsync(string userId, AddBook request);

    Task<TryResult> DeleteBookAsync(string userId, string bookId);
}