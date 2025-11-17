using Intellishelf.Api.Contracts.Chat;
using Intellishelf.Domain.Chat.Models;
using Intellishelf.Domain.Chat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[Authorize]
[Route("chat")]
public class ChatController(IChatService chatService) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequestDto requestDto)
    {
        // Map DTO to Domain model
        var request = new ChatRequest
        {
            Message = requestDto.Message,
            History = requestDto.History?.Select(h => new ChatMessage
            {
                Role = h.Role,
                Content = h.Content
            }).ToArray()
        };

        // Call service
        var result = await chatService.ChatAsync(CurrentUserId, request);

        if (!result.IsSuccess)
            return HandleErrorResponse(result.Error);

        return Ok(result.Value);
    }
}