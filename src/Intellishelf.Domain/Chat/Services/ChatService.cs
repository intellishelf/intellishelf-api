using System.Text.Json;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Chat.Errors;
using Intellishelf.Domain.Chat.Models;
using OpenAI.Chat;

namespace Intellishelf.Domain.Chat.Services;

public class ChatService(ChatClient chatClient, IBookDao bookDao) : IChatService
{
    private const string SystemPromptTemplate = """
        You are an AI assistant helping users manage and explore their personal bookshelf.

        The user's bookshelf contains {0} books:
        {1}

        Answer questions about their books, provide recommendations, track reading progress, and help organize their library.
        Be concise and helpful. If the user asks about books not in their library, you can suggest adding them.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<TryResult<ChatResponse>> SendMessageAsync(string userId, ChatRequest request)
    {
        // Validate request
        if (request.Messages == null || request.Messages.Count == 0)
            return new Error(ChatErrorCodes.EmptyConversation, "Conversation cannot be empty.");

        // Fetch user's books for context
        var booksResult = await bookDao.GetBooksAsync(userId);
        if (!booksResult.IsSuccess)
            return new Error(ChatErrorCodes.AiRequestFailed, "Failed to fetch books context.");

        var books = booksResult.Value;

        // Build books context (lightweight fields only)
        var booksContext = books.Select(b => new
        {
            id = b.Id,
            title = b.Title,
            authors = b.Authors,
            publisher = b.Publisher,
            publicationDate = b.PublicationDate?.ToString("yyyy-MM-dd"),
            isbn10 = b.Isbn10,
            isbn13 = b.Isbn13,
            pages = b.Pages,
            tags = b.Tags,
            status = b.Status.ToString(),
            startedReadingDate = b.StartedReadingDate?.ToString("yyyy-MM-dd"),
            finishedReadingDate = b.FinishedReadingDate?.ToString("yyyy-MM-dd")
        }).ToList();

        var booksJson = JsonSerializer.Serialize(booksContext, JsonOptions);
        var systemPrompt = string.Format(SystemPromptTemplate, books.Count, booksJson);

        // Build OpenAI messages
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt)
        };

        // Add conversation history from request
        foreach (var msg in request.Messages)
        {
            messages.Add(msg.Role.ToLowerInvariant() switch
            {
                "user" => new UserChatMessage(msg.Content),
                "assistant" => new AssistantChatMessage(msg.Content),
                _ => throw new InvalidOperationException($"Invalid message role: {msg.Role}")
            });
        }

        // Call OpenAI
        try
        {
            var response = await chatClient.CompleteChatAsync(messages);
            var content = response.Value.Content[0].Text;

            return new ChatResponse(content, books.Count);
        }
        catch (Exception ex)
        {
            return new Error(ChatErrorCodes.AiRequestFailed, $"AI request failed: {ex.Message}");
        }
    }
}
