using System.Text;
using Intellishelf.Domain.Chat.Models;
using OpenAI.Chat;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;

namespace Intellishelf.Domain.Chat.Services;

public class ChatService(IMcpToolsService mcpToolsService, ChatClient chatClient) : IChatService
{
    public async Task<TryResult<IAsyncEnumerable<ChatStreamChunk>>> ChatStreamAsync(string userId, ChatRequest request)
    {
        // Build system prompt without book data (tools will provide it on demand)
        var systemPrompt = """
            You are a friendly and conversational personal librarian assistant. You have access to the user's book collection through tools.

            Your role is to:
            - Answer questions about their books naturally and conversationally on the language which user uses
            - Provide recommendations based on their collection
            - Help them find specific books or information
            - Discuss their reading progress and history

            Use the available tools to access book information when needed. Be helpful, conversational, and personable in your responses.
            """;

        var messages = new List<OpenAIChatMessage> { OpenAIChatMessage.CreateSystemMessage(systemPrompt) };

        if (request.History is { Length: > 0 })
        {
            foreach (var historyMsg in request.History)
            {
                messages.Add(historyMsg.Role.ToLowerInvariant() switch
                {
                    "user" => OpenAIChatMessage.CreateUserMessage(historyMsg.Content),
                    "assistant" => OpenAIChatMessage.CreateAssistantMessage(historyMsg.Content),
                    _ => OpenAIChatMessage.CreateUserMessage(historyMsg.Content)
                });
            }
        }

        // Add current user message
        messages.Add(OpenAIChatMessage.CreateUserMessage(request.Message));

        // Return the streaming enumerable - errors after this point are mid-stream
        return TryResult.Success(StreamOpenAiResponseAsync(userId, messages));
    }

    private async IAsyncEnumerable<ChatStreamChunk> StreamOpenAiResponseAsync(string userId, List<OpenAIChatMessage> messages)
    {
        // Get MCP tools as OpenAI tool definitions
        var tools = mcpToolsService.GetOpenAiTools();

        var chatOptions = new ChatCompletionOptions();
        foreach (var tool in tools)
        {
            chatOptions.Tools.Add(tool);
        }

        // Tool calling loop - may need multiple iterations if LLM calls tools
        const int maxIterations = 5; // Prevent infinite loops
        var iteration = 0;

        while (iteration < maxIterations)
        {
            iteration++;

            var toolCallAccumulators = new Dictionary<int, ToolCallAccumulator>();
            var hasContent = false;

            // Stream the response and collect tool call updates
            await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, chatOptions))
            {
                // Collect content
                foreach (var contentPart in update.ContentUpdate)
                {
                    hasContent = true;

                    // Stream content to client
                    yield return new ChatStreamChunk
                    {
                        Content = contentPart.Text,
                        Done = false
                    };
                }

                // Accumulate streaming tool call updates (incremental fragments)
                foreach (var toolCallUpdate in update.ToolCallUpdates)
                {
                    if (!toolCallAccumulators.TryGetValue(toolCallUpdate.Index, out var accumulator))
                    {
                        accumulator = new ToolCallAccumulator { Index = toolCallUpdate.Index };
                        toolCallAccumulators[toolCallUpdate.Index] = accumulator;
                    }

                    // Accumulate incremental data
                    if (toolCallUpdate.ToolCallId != null)
                        accumulator.ToolCallId = toolCallUpdate.ToolCallId;

                    if (toolCallUpdate.FunctionName != null)
                        accumulator.FunctionName = toolCallUpdate.FunctionName;

                    if (toolCallUpdate.FunctionArgumentsUpdate != null)
                        accumulator.FunctionArguments.Append(toolCallUpdate.FunctionArgumentsUpdate);
                }

                // Check if we're done (no tool calls)
                if (update.FinishReason == ChatFinishReason.Stop)
                {
                    yield return new ChatStreamChunk
                    {
                        Content = string.Empty,
                        Done = true
                    };
                    yield break;
                }

                // Tool calls requested - finish streaming and execute tools
                if (update.FinishReason == ChatFinishReason.ToolCalls)
                {
                    break;
                }
            }

            // If LLM wants to call tools, execute them
            if (toolCallAccumulators.Count > 0)
            {
                // Convert accumulated updates to complete ChatToolCall objects
                var toolCalls = toolCallAccumulators.Values
                    .Where(acc => acc.FunctionName != null && acc.ToolCallId != null)
                    .OrderBy(acc => acc.Index)
                    .Select(acc => ChatToolCall.CreateFunctionToolCall(
                        acc.ToolCallId!,
                        acc.FunctionName!,
                        BinaryData.FromString(acc.FunctionArguments.ToString())))
                    .ToList();

                if (toolCalls.Count == 0)
                {
                    // Invalid tool calls - bail out
                    yield return new ChatStreamChunk
                    {
                        Content = string.Empty,
                        Done = true
                    };
                    yield break;
                }

                // Add assistant message with tool calls
                messages.Add(OpenAIChatMessage.CreateAssistantMessage(toolCalls));

                // Execute each tool call
                foreach (var toolCall in toolCalls)
                {
                    // Notify client that tool is being called with friendly description
                    var description = ToolCallFormatter.FormatToolCall(
                        toolCall.FunctionName,
                        toolCall.FunctionArguments.ToString());

                    yield return new ChatStreamChunk
                    {
                        Type = ChunkType.ToolCall,
                        ToolCallDescription = description,
                        Content = string.Empty,
                        Done = false
                    };

                    var toolResult = await mcpToolsService.ExecuteToolAsync(
                        userId,
                        toolCall.FunctionName,
                        toolCall.FunctionArguments.ToString());

                    // Add tool result message
                    messages.Add(OpenAIChatMessage.CreateToolMessage(toolCall.Id, toolResult));
                }

                // Continue the loop to get the final response with tool results
                continue;
            }

            // No tool calls and no explicit stop - shouldn't happen, but break to be safe
            yield return new ChatStreamChunk
            {
                Content = string.Empty,
                Done = true
            };
            yield break;
        }

        // Max iterations reached
        yield return new ChatStreamChunk
        {
            Content = string.Empty,
            Done = true
        };
    }

    private class ToolCallAccumulator
    {
        public required int Index { get; init; }
        public string? ToolCallId { get; set; }
        public string? FunctionName { get; set; }
        public StringBuilder FunctionArguments { get; } = new();
    }
}