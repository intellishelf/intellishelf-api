using OpenAI.Chat;

namespace Intellishelf.Domain.Chat.Services;

public interface IMcpToolsService
{
    /// <summary>
    /// Get all available MCP tools as OpenAI ChatTool definitions
    /// </summary>
    IReadOnlyList<ChatTool> GetOpenAiTools();

    /// <summary>
    /// Execute an MCP tool call and return the result as JSON
    /// </summary>
    Task<string> ExecuteToolAsync(string userId, string toolName, string argumentsJson);
}
