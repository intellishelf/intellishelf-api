using Intellishelf.Api.Contracts.Books;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Api.Mappers.Books;

public interface IBookMapper
{
    AddBookRequest MapAdd(string userId, AddBookRequestContract contract);
    DeleteBookRequest MapDelete(string userId, string bookId);
}