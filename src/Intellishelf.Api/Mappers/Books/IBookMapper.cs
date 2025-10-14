using Intellishelf.Api.Contracts.Books;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Api.Mappers.Books;

public interface IBookMapper
{
    AddBookRequest MapAdd(string userId, BookRequestContractBase contract, string? coverImageUrl);
    UpdateBookRequest MapUpdate(string userId, string bookId, BookRequestContractBase contract, string? coverImageUrl);
}