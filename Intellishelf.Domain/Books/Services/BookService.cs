using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Files.Services;

namespace Intellishelf.Domain.Books.Services;

public class BookService(IBookDao bookDao, IFileStorageService fileStorageService) : IBookService
{
    public async Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId) =>
        await bookDao.GetBooksAsync(userId);

    public async Task<TryResult<PagedResult<Book>>> TryGetPagedBooksAsync(string userId, BookQueryParameters queryParameters) =>
        await bookDao.GetPagedBooksAsync(userId, queryParameters);

    public async Task<TryResult<Book>> TryGetBookAsync(string userId, string bookId) =>
        await bookDao.GetBookAsync(userId, bookId);

    public async Task<TryResult<Book>> TryAddBookAsync(AddBookRequest request) =>
        await bookDao.AddBookAsync(request);

    public async Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request)
    {
        var existingBook = await TryGetBookAsync(request.UserId, request.Id);

        if (request.CoverImageUrl != null && existingBook.IsSuccess &&
            !string.IsNullOrEmpty(existingBook.Value.CoverImageUrl))
        {
            await fileStorageService.DeleteFileFromUrlAsync(existingBook.Value.CoverImageUrl);
        }

        return await bookDao.TryUpdateBookAsync(request);
    }

    public async Task<TryResult> TryDeleteBookAsync(DeleteBookRequest request)
    {
        var existingBook = await TryGetBookAsync(request.UserId, request.BookId);

        if (existingBook.IsSuccess && !string.IsNullOrEmpty(existingBook.Value.CoverImageUrl))
        {
            await fileStorageService.DeleteFileFromUrlAsync(existingBook.Value.CoverImageUrl);
        }

        return await bookDao.DeleteBookAsync(request);
    }
}
