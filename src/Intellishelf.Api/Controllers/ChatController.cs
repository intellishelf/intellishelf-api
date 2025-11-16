using Intellishelf.Domain.Chat.Models;
using Intellishelf.Domain.Chat.Services;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(IChatService chatService) : ApiControllerBase
{
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        var result = await chatService.SendMessageAsync(CurrentUserId, request);

        if (!result.IsSuccess)
            return HandleErrorResponse(result.Error);

        return Ok(result.Value);
    }
}
