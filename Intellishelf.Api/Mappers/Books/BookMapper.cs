using Intellishelf.Api.Contracts.Books;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Api.Mappers.Books;

public class BookMapper : IBookMapper
{
    public AddBookRequest MapAdd(string userId, BookRequestContractBase contract, string? coverImageUrl) =>
        new()
        {
            UserId = userId,
            Title = contract.Title,
            Annotation = contract.Annotation,
            Authors = contract.Authors,
            Description = contract.Description,
            Isbn = contract.Isbn,
            Pages = contract.Pages,
            PublicationDate = contract.PublicationDate,
            Publisher = contract.Publisher,
            Tags = contract.Tags,
            CoverImageUrl = coverImageUrl
        };

    public UpdateBookRequest MapUpdate(string userId, string bookId, BookRequestContractBase contract, string? coverImageUrl) =>
        new()
        {
            Id = bookId,
            UserId = userId,
            Title = contract.Title,
            Annotation = contract.Annotation,
            Authors = contract.Authors,
            Description = contract.Description,
            Isbn = contract.Isbn,
            Pages = contract.Pages,
            PublicationDate = contract.PublicationDate,
            Publisher = contract.Publisher,
            Tags = contract.Tags,
            CoverImageUrl = coverImageUrl
        };
}