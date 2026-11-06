using Intellishelf.Data.Books.Entities;
using Intellishelf.Domain.Books.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Intellishelf.Unit.Tests.Books;

public class BookEntityTests
{
    [Fact]
    public void Deserialize_WithLegacySearchText_PreservesBook()
    {
        var book = new BookEntity
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Existing book",
            Status = ReadingStatus.Unread,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        var document = book.ToBsonDocument();
        document.Add("SearchText", "existing book");

        var result = BsonSerializer.Deserialize<BookEntity>(document);

        Assert.Equal(book.Title, result.Title);
        Assert.Equal(book.UserId, result.UserId);
    }
}
