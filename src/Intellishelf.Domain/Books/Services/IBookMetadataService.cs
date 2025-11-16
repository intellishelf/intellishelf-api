using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Books.Services;

public interface IBookMetadataService
{
    Task<TryResult<BookMetadata>> TryGetBookMetadataAsync(string isbn);
}
