using Intellishelf.Api.Contracts.Books;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Api.Mappers.Books;

public class BookMapper : IBookMapper
{
    public AddBookRequest MapAdd(string userId, BookRequestContractBase contract) =>
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

    public UpdateBookRequest MapUpdate(string userId, string bookId, BookRequestContractBase contract) =>
        new()
        {
            Id = bookId,
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