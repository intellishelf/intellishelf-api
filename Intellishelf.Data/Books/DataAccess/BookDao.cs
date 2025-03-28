using Intellishelf.Common.TryResult;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Models;
using MongoDB.Driver;

namespace Intellishelf.Data.Books.DataAccess;

public class BookDao(IMongoDatabase database) : IBookDao
{
    private readonly IMongoCollection<BookEntity> _booksCollection = database.GetCollection<BookEntity>("Books");

    public async Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId)
    {
        var books = await _booksCollection
            .Find(b => b.UserId == userId)
            .ToListAsync();

        var result = books.Select(b => new Book
        {
            Id = b.Id,
            Title = b.Title,
            Authors = b.Authors,
            UserId = b.UserId,
            Description = b.Description,
            Isbn = b.Isbn,
            Pages = b.Pages,
            Annotation = b.Annotation,
            PublicationDate = b.PublicationDate,
            Publisher = b.Publisher,
            ImageUrl = b.ImageUrl,
            CreatedDate = b.CreatedDate,
            Tags = b.Tags
        }).ToList();

        return TryResult.Success<IReadOnlyCollection<Book>>(result);
    }

    public async Task<TryResult> AddBookAsync(AddBook request)
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

    public async Task<TryResult> DeleteBookAsync(string userId, string bookId)
    {
        await _booksCollection.DeleteOneAsync(b => b.Id == bookId && b.UserId == userId);

        return TryResult.Success();
    }
}