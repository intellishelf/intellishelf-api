using System.Text.Json;
using Intellishelf.Api.Mcp.Tools;
using Intellishelf.Domain.Chat.Services;
using OpenAI.Chat;

namespace Intellishelf.Api.Services;

public class McpToolsService(GetAllBooksTool getAllBooksTool, GetBooksByAuthorTool getBooksByAuthorTool)
    : IMcpToolsService
{
    private static readonly ChatTool GetAllBooksToolDefinition = ChatTool.CreateFunctionTool(
        functionName: "get_all_books",
        functionDescription: "Get all books from the user's library. Returns the complete collection with title, authors, publication info, reading status, and tags.");

    private static readonly ChatTool GetBooksByAuthorToolDefinition = ChatTool.CreateFunctionTool(
        functionName: "get_books_by_author",
        functionDescription: "Get books by a specific author from the user's library. Performs case-insensitive partial matching on author names.",
        functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {
                    "author": {
                        "type": "string",
                        "description": "Author name to filter by (case-insensitive, partial match)"
                    }
                },
                "required": ["author"],
                "additionalProperties": false
            }
            """));

    public IReadOnlyList<ChatTool> GetOpenAiTools()
    {
        return [GetAllBooksToolDefinition, GetBooksByAuthorToolDefinition];
    }

    public async Task<string> ExecuteToolAsync(string userId, string toolName, string argumentsJson)
    {
        return toolName switch
        {
            "get_all_books" => await ExecuteGetAllBooksAsync(userId),
            "get_books_by_author" => await ExecuteGetBooksByAuthorAsync(userId, argumentsJson),
            _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
        };
    }

    private async Task<string> ExecuteGetAllBooksAsync(string userId)
    {
        var books = await getAllBooksTool.GetAllBooks(userId);
        return JsonSerializer.Serialize(books);
    }

    private async Task<string> ExecuteGetBooksByAuthorAsync(string userId, string argumentsJson)
    {
        var args = JsonSerializer.Deserialize<GetBooksByAuthorArgs>(argumentsJson)
            ?? throw new InvalidOperationException("Invalid arguments for get_books_by_author");

        var books = await getBooksByAuthorTool.GetBooksByAuthor(userId, args.Author);
        return JsonSerializer.Serialize(books);
    }

    private record GetBooksByAuthorArgs(string Author);
}
