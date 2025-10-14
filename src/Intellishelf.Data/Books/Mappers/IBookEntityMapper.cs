using Intellishelf.Data.Books.Entities;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Data.Books.Mappers;

public interface IBookEntityMapper
{
    Book Map(BookEntity bookEntity);
}