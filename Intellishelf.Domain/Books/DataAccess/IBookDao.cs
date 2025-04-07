using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.DataAccess;

public interface IBookDao
{
    Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId);

    Task<TryResult<Book>> GetBookAsync(string userId, string bookId);

    Task<TryResult<Book>> AddBookAsync(AddBookRequest request);

    Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request);

    Task<TryResult> DeleteBookAsync(DeleteBookRequest request);
}