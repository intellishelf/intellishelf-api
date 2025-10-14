using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;

namespace Intellishelf.Domain.Ai.Services;

public interface IAiService
{
    Task<TryResult<ParsedBook>> ParseBookFromTextAsync(string text, bool useMockedAi);
}