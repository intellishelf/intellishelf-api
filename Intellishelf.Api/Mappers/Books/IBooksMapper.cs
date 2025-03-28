using Intellishelf.Api.Contracts.Books;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Api.Mappers.Books;

public interface IBooksMapper
{
    AddBook MapAdd(string userId, AddBookContract contract);
    DeleteBook MapDelete(string userId, string bookId);
}