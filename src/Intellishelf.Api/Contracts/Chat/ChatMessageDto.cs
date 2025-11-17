namespace Intellishelf.Api.Contracts.Chat;

public class ChatMessageDto
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}
