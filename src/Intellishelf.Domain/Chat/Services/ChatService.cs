using System.Text.Json;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Chat.Errors;
using Intellishelf.Domain.Chat.Models;
using OpenAI.Chat;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;

namespace Intellishelf.Domain.Chat.Services;

public class ChatService(IBookDao bookDao, ChatClient chatClient) : IChatService
{
    public async IAsyncEnumerable<ChatStreamChunk> ChatStreamAsync(string userId, ChatRequest request)
    {
        // Retrieve user's books
        var booksResult = await bookDao.GetBooksAsync(userId);
        if (!booksResult.IsSuccess)
        {
            yield return new ChatStreamChunk
            {
                Content = string.Empty,
                Done = true,
                Error = booksResult.Error.Message
            };
            yield break;
        }

        var books = booksResult.Value;

        // Project to lightweight context (exclude heavy properties)
        var bookContexts = books.Select(b => new BookChatContext
        {
            Id = b.Id,
            Title = b.Title,
            Authors = b.Authors,
            Publisher = b.Publisher,
            PublicationDate = b.PublicationDate,
            Pages = b.Pages,
            Isbn10 = b.Isbn10,
            Isbn13 = b.Isbn13,
            Status = b.Status.ToString(),
            StartedReadingDate = b.StartedReadingDate,
            FinishedReadingDate = b.FinishedReadingDate,
            Tags = b.Tags
        }).ToList();

        var booksJson = JsonSerializer.Serialize(bookContexts, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var systemPrompt = $"""
            You are a friendly and conversational personal librarian assistant. You have access to the user's complete book collection.

            Your role is to:
            - Answer questions about their books naturally and conversationally
            - Provide recommendations based on their collection
            - Help them find specific books or information
            - Discuss their reading progress and history

            Here is their book collection (in JSON format):

            {booksJson}

            Be helpful, conversational, and personable in your responses. When discussing books, use natural language rather than just listing data.
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

        // Stream the response from OpenAI
        var streamingEnumerable = StreamOpenAiResponseAsync(messages);
        await foreach (var chunk in streamingEnumerable)
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<ChatStreamChunk> StreamOpenAiResponseAsync(List<OpenAIChatMessage> messages)
    {
        ChatStreamChunk? errorChunk = null;

        try
        {
            await foreach (var update in chatClient.CompleteChatStreamingAsync(messages))
            {
                foreach (var contentPart in update.ContentUpdate)
                {
                    yield return new ChatStreamChunk
                    {
                        Content = contentPart.Text,
                        Done = false
                    };
                }
            }
        }
        catch (Exception ex)
        {
            errorChunk = new ChatStreamChunk
            {
                Content = string.Empty,
                Done = true,
                Error = $"AI request failed: {ex.Message}"
            };
        }

        // Send final chunk - either success or error
        yield return errorChunk ?? new ChatStreamChunk
        {
            Content = string.Empty,
            Done = true
        };
    }
}