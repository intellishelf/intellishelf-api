namespace Intellishelf.Api.Contracts.Chat;

public class ChatRequestDto
{
    public required string Message { get; init; }
    public ChatMessageDto[]? History { get; init; }
}
