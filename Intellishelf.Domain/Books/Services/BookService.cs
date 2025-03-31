using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public class BookService(IBookDao bookDao) : IBookService
{
    public async Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId) =>
        await bookDao.GetBooksAsync(userId);

    public async Task<TryResult<Book>> TryGetBookAsync(string userId, string bookId) =>
        await bookDao.GetBookAsync(userId, bookId);

    public async Task<TryResult> TryAddBookAsync(AddBookRequest request) =>
        await bookDao.AddBookAsync(request);

    public async Task<TryResult> TryDeleteBookAsync(DeleteBookRequest request) =>
        await bookDao.DeleteBookAsync(request);
}