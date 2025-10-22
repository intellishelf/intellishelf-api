using Intellishelf.Common.TryResult;
using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Books.Mappers;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Books.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intellishelf.Data.Books.DataAccess;

public class BookDao(IMongoDatabase database, IBookEntityMapper mapper) : IBookDao
{
    private readonly IMongoCollection<BookEntity> _booksCollection = database.GetCollection<BookEntity>(BookEntity.CollectionName);

    public async Task<TryResult<IReadOnlyCollection<Book>>> GetBooksAsync(string userId)
    {
        var userIdObject = ObjectId.Parse(userId);

        var books = await _booksCollection
            .Find(b => b.UserId == userIdObject)
            .ToListAsync();

        var result = books.Select(mapper.Map).ToList();

        return result;
    }

    public async Task<TryResult<PagedResult<Book>>> GetPagedBooksAsync(string userId, BookQueryParameters queryParameters)
    {
        var filter = Builders<BookEntity>.Filter.Eq(b => b.UserId, ObjectId.Parse(userId));

        // Get total count for pagination
        var totalCount = await _booksCollection.CountDocumentsAsync(filter);

        // Build sort definition based on orderBy parameter
        var sortDefinition = BuildSortDefinition(queryParameters.OrderBy, queryParameters.Ascending);

        // Get paged data
        var books = await _booksCollection
            .Find(filter)
            .Sort(sortDefinition)
            .Skip((queryParameters.Page - 1) * queryParameters.PageSize)
            .Limit(queryParameters.PageSize)
            .ToListAsync();

        var mappedBooks = books.Select(mapper.Map).ToList();

        var pagedResult = new PagedResult<Book>(
            mappedBooks,
            totalCount,
            queryParameters.Page,
            queryParameters.PageSize);

        return pagedResult;
    }

    private static SortDefinition<BookEntity> BuildSortDefinition(BookOrderBy orderBy, bool ascending)
    {
        SortDefinition<BookEntity> sortDefinition;

        switch (orderBy)
        {
            case BookOrderBy.Title:
                sortDefinition = ascending
                    ? Builders<BookEntity>.Sort.Ascending(b => b.Title)
                    : Builders<BookEntity>.Sort.Descending(b => b.Title);
                break;
            case BookOrderBy.Author:
                // Sort by the last author in the array if it exists
                sortDefinition = ascending
                    ? Builders<BookEntity>.Sort.Ascending(b => b.Authors != null && b.Authors.Length > 0 ? b.Authors.FirstOrDefault() : string.Empty)
                    : Builders<BookEntity>.Sort.Descending(b => b.Authors != null && b.Authors.Length > 0 ? b.Authors.FirstOrDefault() : string.Empty);
                break;
            case BookOrderBy.Published:
                sortDefinition = ascending
                    ? Builders<BookEntity>.Sort.Ascending(b => b.PublicationDate)
                    : Builders<BookEntity>.Sort.Descending(b => b.PublicationDate);
                break;
            case BookOrderBy.Added:
            default:
                sortDefinition = ascending
                    ? Builders<BookEntity>.Sort.Ascending(b => b.CreatedDate)
                    : Builders<BookEntity>.Sort.Descending(b => b.CreatedDate);
                break;
        }

        return sortDefinition;
    }

    public async Task<TryResult<Book>> GetBookAsync(string userId, string bookId)
    {
        var userIdObject = ObjectId.Parse(userId);

        var bookEntity = await _booksCollection
            .Find(b => b.Id == bookId && b.UserId == userIdObject)
            .FirstOrDefaultAsync();

        if (bookEntity == null)
            return new Error(BookErrorCodes.BookNotFound, "Book not found");

        return mapper.Map(bookEntity);
    }

    public async Task<TryResult<Book>> AddBookAsync(AddBookRequest request)
    {
        var book = new BookEntity
        {
            UserId = ObjectId.Parse(request.UserId),
            Title = request.Title,
            Authors = request.Authors,
            PublicationDate = request.PublicationDate,
            Isbn10 = request.Isbn10,
            Isbn13 = request.Isbn13,
            Tags = request.Tags,
            Annotation = request.Annotation,
            Description = request.Description,
            Publisher = request.Publisher,
            Pages = request.Pages,
            CoverImageUrl = request.CoverImageUrl,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        await _booksCollection.InsertOneAsync(book);

        return mapper.Map(book);
    }

    public async Task<TryResult> TryUpdateBookAsync(UpdateBookRequest request)
    {
        var userIdObject = ObjectId.Parse(request.UserId);

        var filter = Builders<BookEntity>
            .Filter
            .Where(b => b.Id == request.Id && b.UserId == userIdObject);

        var updates = new List<UpdateDefinition<BookEntity>>
        {
            Builders<BookEntity>.Update.Set(b => b.Title, request.Title),
            Builders<BookEntity>.Update.Set(b => b.Authors, request.Authors),
            Builders<BookEntity>.Update.Set(b => b.PublicationDate, request.PublicationDate),
            Builders<BookEntity>.Update.Set(b => b.Isbn10, request.Isbn10),
            Builders<BookEntity>.Update.Set(b => b.Isbn13, request.Isbn13),
            Builders<BookEntity>.Update.Set(b => b.Tags, request.Tags),
            Builders<BookEntity>.Update.Set(b => b.Annotation, request.Annotation),
            Builders<BookEntity>.Update.Set(b => b.Description, request.Description),
            Builders<BookEntity>.Update.Set(b => b.Publisher, request.Publisher),
            Builders<BookEntity>.Update.Set(b => b.Pages, request.Pages),
            Builders<BookEntity>.Update.CurrentDate(b => b.ModifiedDate)
        };

        if (request.CoverImageUrl != null)
        {
            updates.Add(
                Builders<BookEntity>
                    .Update
                    .Set(b => b.CoverImageUrl, request.CoverImageUrl));
        }

        var combinedUpdate = Builders<BookEntity>.Update.Combine(updates);

        var result = await _booksCollection.UpdateOneAsync(filter, combinedUpdate);

        return result.MatchedCount == 0
            ? new Error(BookErrorCodes.BookNotFound, "Book not found or no permission to update")
            : TryResult.Success();
    }

    public async Task<TryResult> DeleteBookAsync(string userId, string bookId)
    {
        var userIdObject = ObjectId.Parse(userId);

        var result = await _booksCollection.DeleteOneAsync(b => b.Id == bookId && b.UserId == userIdObject);

        return result.DeletedCount == 0
            ? new Error(BookErrorCodes.BookNotFound, "Book not found or no permission to delete")
            : TryResult.Success();
    }

    public async Task<TryResult<IReadOnlyCollection<Book>>> SearchAsync(string userId, string searchTerm)
    {
        var scoreBuilder = Builders<BookEntity>.SearchScore;

        var searchFilter = Builders<BookEntity>
            .Search
            .Compound()
            .Filter(Builders<BookEntity>.Search.Equals(f => f.UserId, ObjectId.Parse(userId)))
            .Should(
                Builders<BookEntity>.Search.Text(f => f.Title, searchTerm, score: scoreBuilder.Constant(3.0)),
                Builders<BookEntity>.Search.Text(f => f.Authors, searchTerm, score: scoreBuilder.Constant(3.0)),
                Builders<BookEntity>.Search.Text(f => f.Tags, searchTerm, score: scoreBuilder.Constant(2.0)),
                Builders<BookEntity>.Search.Text(f => f.Description, searchTerm, score: scoreBuilder.Constant(1.0)),
                Builders<BookEntity>.Search.Text(f => f.Annotation, searchTerm, score: scoreBuilder.Constant(1.0)),
                Builders<BookEntity>.Search.Text(f => f.Publisher, searchTerm, score: scoreBuilder.Constant(1.0))
            )
            .MinimumShouldMatch(1);

        var result = await _booksCollection
            .Aggregate()
            .Search(searchFilter)
            .ToListAsync();

        return result.Select(mapper.Map).ToList();
    }
}