namespace Intellishelf.Domain.Chat.Models;

public class ChatRequest
{
    public required string Message { get; init; }
    public ChatMessage[]? History { get; init; }
}
