using Intellishelf.Data.Books.Entities;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Data.Books.Mappers;

public class BookEntityMapper : IBookEntityMapper
{
    public Book Map(BookEntity bookEntity) =>
        new()
        {
            Id = bookEntity.Id,
            Title = bookEntity.Title,
            Authors = bookEntity.Authors,
            UserId = bookEntity.UserId,
            Description = bookEntity.Description,
            Isbn = bookEntity.Isbn,
            Pages = bookEntity.Pages,
            Annotation = bookEntity.Annotation,
            PublicationDate = bookEntity.PublicationDate,
            Publisher = bookEntity.Publisher,
            ImageUrl = bookEntity.ImageUrl,
            CreatedDate = bookEntity.CreatedDate,
            Tags = bookEntity.Tags
        };
}