using Intellishelf.Domain.Chat.Models;

namespace Intellishelf.Domain.Chat.Services;

public interface IChatService
{
    Task<TryResult<IAsyncEnumerable<ChatStreamChunk>>> ChatStreamAsync(string userId, ChatRequest request);
}
