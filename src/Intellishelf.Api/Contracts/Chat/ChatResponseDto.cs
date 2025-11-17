namespace Intellishelf.Api.Contracts.Chat;

public class ChatResponseDto
{
    public required string Message { get; init; }
    public int TokensUsed { get; init; }
}
