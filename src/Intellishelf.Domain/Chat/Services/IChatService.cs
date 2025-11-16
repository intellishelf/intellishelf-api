using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Chat.Models;

namespace Intellishelf.Domain.Chat.Services;

public interface IChatService
{
    Task<TryResult<ChatResponse>> SendMessageAsync(string userId, ChatRequest request);
}
