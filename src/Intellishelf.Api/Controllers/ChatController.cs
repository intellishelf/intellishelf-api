using System.Text;
using System.Text.Json;
using Intellishelf.Api.Contracts.Chat;
using Intellishelf.Domain.Chat.Models;
using Intellishelf.Domain.Chat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[Authorize]
[Route("chat-stream")]
public class ChatController(IChatService chatService) : ApiControllerBase
{
    [HttpPost]
    public async Task ChatStream([FromBody] ChatRequestDto requestDto, CancellationToken cancellationToken)
    {
        // Set SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
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

        // Stream the response
        await foreach (var chunk in chatService.ChatStreamAsync(CurrentUserId, request).WithCancellation(cancellationToken))
        {
            var json = JsonSerializer.Serialize(chunk);
            var sseMessage = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(sseMessage);

            await Response.Body.WriteAsync(bytes, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            if (chunk.Done)
                break;
        }
    }
}