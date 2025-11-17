using Intellishelf.Domain.Chat.Models;

namespace Intellishelf.Domain.Chat.Services;

public interface IChatService
{
    IAsyncEnumerable<ChatStreamChunk> ChatStreamAsync(string userId, ChatRequest request);
}
