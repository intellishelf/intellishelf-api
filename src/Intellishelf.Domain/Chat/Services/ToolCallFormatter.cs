using System.Text.Json;

namespace Intellishelf.Domain.Chat.Services;

/// <summary>
/// Formats tool calls into human-friendly descriptions for display
/// </summary>
public static class ToolCallFormatter
{
    public static string FormatToolCall(string toolName, string? argumentsJson)
    {
        return toolName switch
        {
            "get_all_books" => "Looking through your library...",
            "get_books_by_author" => FormatGetBooksByAuthor(argumentsJson),
            _ => $"Using {toolName}..."
        };
    }

    private static string FormatGetBooksByAuthor(string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
            return "Searching for books...";

        try
        {
            var args = JsonSerializer.Deserialize<GetBooksByAuthorArgs>(
                argumentsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (args?.Author != null)
                return $"Searching books by {args.Author}...";
        }
        catch
        {
            // Fallback if JSON parsing fails
        }

        return "Searching for books...";
    }

    private record GetBooksByAuthorArgs(string? Author);
}
