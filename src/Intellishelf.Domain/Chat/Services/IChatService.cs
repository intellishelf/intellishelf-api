using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Chat.Models;

namespace Intellishelf.Domain.Chat.Services;

public interface IChatService
{
    Task<TryResult<ChatResponse>> ChatAsync(string userId, ChatRequest request);
}
