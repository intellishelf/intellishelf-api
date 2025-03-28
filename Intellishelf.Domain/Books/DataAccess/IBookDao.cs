using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.DataAccess;

public interface IBookDao
{
    Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId);

    Task<TryResult> AddBookAsync(string userId, AddBook request);

    Task<TryResult> DeleteBookAsync(string userId, string bookId);
}