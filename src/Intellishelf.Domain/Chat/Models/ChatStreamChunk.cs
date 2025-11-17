namespace Intellishelf.Domain.Chat.Models;

public class ChatStreamChunk
{
    public string Content { get; init; } = string.Empty;
    public bool Done { get; init; }
    public string? Error { get; init; }
}
