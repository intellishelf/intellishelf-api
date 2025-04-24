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
            Isbn = contract.Isbn,
            Pages = contract.Pages,
            PublicationDate = contract.PublicationDate,
            Publisher = contract.Publisher,
            Tags = contract.Tags,
            BookCover = contract.ImageFile == null
                ? null
                : new BookCover(GetUniqueFileName(contract.ImageFile.FileName), contract.ImageFile.OpenReadStream())
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
            Isbn = contract.Isbn,
            Pages = contract.Pages,
            PublicationDate = contract.PublicationDate,
            Publisher = contract.Publisher,
            Tags = contract.Tags,
            BookCover = contract.ImageFile == null
                ? null
                : new BookCover(GetUniqueFileName(contract.ImageFile.FileName), contract.ImageFile.OpenReadStream())
        };

    public DeleteBookRequest MapDelete(string userId, string bookId) =>
        new(userId, bookId);

    private static string GetUniqueFileName(string fileName) => $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
}