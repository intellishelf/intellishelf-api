namespace Intellishelf.Domain.Chat.Models;

public class ChatResponse
{
    public required string Message { get; init; }
    public int TokensUsed { get; init; }
}
