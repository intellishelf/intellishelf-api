using Intellishelf.Api.Contracts.Books;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Api.Mappers.Books;

public interface IBookMapper
{
    AddBookRequest MapAdd(string userId, BookRequestContractBase contract);
    UpdateBookRequest MapUpdate(string userId, string bookId, BookRequestContractBase contract);
    DeleteBookRequest MapDelete(string userId, string bookId);
}