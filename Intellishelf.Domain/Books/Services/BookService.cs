using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Files.Services;

namespace Intellishelf.Domain.Books.Services;

public class BookService(IBookDao bookDao, IFileStorageService fileStorageService) : IBookService
{
    public async Task<TryResult<IReadOnlyCollection<Book>>> TryGetBooksAsync(string userId) =>
        await bookDao.GetBooksAsync(userId);

    public async Task<TryResult<Book>> TryGetBookAsync(string userId, string bookId) =>
        await bookDao.GetBookAsync(userId, bookId);

    public async Task<TryResult<Book>> TryAddBookAsync(AddBookRequest request)
    {
        var result = await bookDao.AddBookAsync(request);

        if (result.IsSuccess && request.BookCover != null)
        {
            var uploadResult = await fileStorageService.UploadFileAsync(
                request.UserId,
                request.BookCover.Content,
                request.BookCover.FileName);

            if (!uploadResult.IsSuccess)
            {
                await TryDeleteBookAsync(new DeleteBookRequest(request.UserId, result.Value.Id));

                return uploadResult.Error;
            }
        }

        return result;
    }

    public async Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request)
    {
        var existingBook = await TryGetBookAsync(request.UserId, request.Id);

        if (request.BookCover != null)
        {
            var uploadResult = await fileStorageService.UploadFileAsync(
                request.UserId,
                request.BookCover.Content,
                request.BookCover.FileName);

            if (!uploadResult.IsSuccess)
            {
                return uploadResult;
            }
        }

        var result = await bookDao.TryUpdateBookAsync(request);

        if (result.IsSuccess && !string.IsNullOrEmpty(existingBook.Value?.FileName) && request.BookCover != null)
        {
            await fileStorageService.DeleteFileAsync(request.UserId, existingBook.Value.FileName);
        }

        if (!result.IsSuccess && request.BookCover != null)
        {
            await fileStorageService.DeleteFileAsync(request.UserId, request.BookCover.FileName);
        }

        return result;
    }

    public async Task<TryResult> TryDeleteBookAsync(DeleteBookRequest request)
    {
        var existingBook = await TryGetBookAsync(request.UserId, request.BookId);

        if (existingBook.IsSuccess && !string.IsNullOrEmpty(existingBook.Value.FileName))
        {
            await fileStorageService.DeleteFileAsync(request.UserId, existingBook.Value.FileName);
        }

        return await bookDao.DeleteBookAsync(request);
    }
}