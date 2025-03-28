using Intellishelf.Api.Contracts.Books;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Api.Mappers.Books;

public class BooksMapper : IBooksMapper
{
    public AddBookRequest MapAdd(string userId, AddBookRequestContract contract) =>
        new()
        {
            UserId = userId,
            Title = contract.Title,
            Annotation = contract.Annotation,
            Authors = contract.Authors,
            Description = contract.Description,
            ImageUrl = contract.ImageUrl,
            Isbn = contract.Isbn,
            Pages = contract.Pages,
            PublicationDate = contract.PublicationDate,
            Publisher = contract.Publisher,
            Tags = contract.Tags
        };

    public DeleteBookRequest MapDelete(string userId, string bookId) =>
        new(userId, bookId);
}