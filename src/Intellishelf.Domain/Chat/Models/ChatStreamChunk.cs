namespace Intellishelf.Domain.Chat.Models;

public class ChatStreamChunk
{
    public required string Content { get; init; }
    public bool Done { get; init; }
}
