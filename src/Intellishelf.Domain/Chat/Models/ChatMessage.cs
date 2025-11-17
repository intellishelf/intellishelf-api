namespace Intellishelf.Domain.Chat.Models;

public class ChatMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}
