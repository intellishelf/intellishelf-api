using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public class BookService(IBookDao bookDao) : IBookService
{
    public async Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId) =>
        await bookDao.GetBooksAsync(userId);

    public async Task<TryResult> AddBookAsync(string userId, AddBook request) =>
        await bookDao.AddBookAsync(userId, request);

    public async Task<TryResult> DeleteBookAsync(string userId, string bookId) =>
        await bookDao.DeleteBookAsync(userId, bookId);
}
