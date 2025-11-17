using System.Text;
using System.Text.Json;
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

    [HttpPost("stream")]
    public async Task ChatStream([FromBody] ChatRequestDto requestDto, CancellationToken cancellationToken)
    {
        // Set streaming headers
        Response.ContentType = "application/json";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

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

        // Stream the response - simple newline-delimited JSON
        await foreach (var chunk in chatService.ChatStreamAsync(CurrentUserId, request).WithCancellation(cancellationToken))
        {
            var json = JsonSerializer.Serialize(chunk);
            var bytes = Encoding.UTF8.GetBytes(json + "\n");

            await Response.Body.WriteAsync(bytes, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}