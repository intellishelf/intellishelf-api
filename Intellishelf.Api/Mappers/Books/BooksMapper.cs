using Intellishelf.Api.Contracts.Books;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Api.Mappers.Books;

public class BooksMapper : IBooksMapper
{
    public AddBook MapAdd(string userId, AddBookContract contract) =>
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

    public DeleteBook MapDelete(string userId, string bookId) =>
        new(userId, bookId);
}