using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public class BookService(IBookDao bookDao) : IBookService
{
    public async Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId) =>
        await bookDao.GetBooksAsync(userId);

    public async Task<TryResult> TryAddBookAsync(AddBook request) =>
        await bookDao.AddBookAsync(request);

    public async Task<TryResult> TryDeleteBookAsync(DeleteBook request) =>
        await bookDao.DeleteBookAsync(request.UserId, request.BookId);
}