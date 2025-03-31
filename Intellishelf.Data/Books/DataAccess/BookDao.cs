using Intellishelf.Common.TryResult;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Books.Mappers;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Models;
using MongoDB.Driver;

namespace Intellishelf.Data.Books.DataAccess;

public class BookDao(IMongoDatabase database, IBookEntityMapper mapper) : IBookDao
{
    private readonly IMongoCollection<BookEntity> _booksCollection = database.GetCollection<BookEntity>("Books");

    public async Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId)
    {
        var books = await _booksCollection
            .Find(b => b.UserId == userId)
            .ToListAsync();

        var result = books.Select(mapper.Map).ToList();

        return result;
    }

    public async Task<TryResult<Book>> GetBookAsync(string userId, string bookId)
    {
        var bookEntity = await _booksCollection
            .Find(b => b.UserId == userId && b.Id == bookId)
            .FirstOrDefaultAsync();

        return mapper.Map(bookEntity);
    }

    public async Task<TryResult> AddBookAsync(AddBookRequest request)
    {
        var book = new BookEntity
        {
            UserId = request.UserId,
            Title = request.Title,
            Authors = request.Authors,
            PublicationDate = request.PublicationDate,
            Isbn = request.Isbn,
            Tags = request.Tags,
            Annotation = request.Annotation,
            Description = request.Description,
            Publisher = request.Publisher,
            Pages = request.Pages,
            ImageUrl = request.ImageUrl,
            CreatedDate = DateTime.UtcNow
        };

        await _booksCollection.InsertOneAsync(book);

        return TryResult.Success();
    }

    public async Task<TryResult> DeleteBookAsync(DeleteBookRequest request)
    {
        await _booksCollection.DeleteOneAsync(b => b.Id == request.BookId && b.UserId == request.UserId);

        return TryResult.Success();
    }
}