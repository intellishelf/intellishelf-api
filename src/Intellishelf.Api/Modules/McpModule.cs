using Intellishelf.Api.Mcp.Tools;
using Intellishelf.Api.Services;
using Intellishelf.Domain.Chat.Services;
using ModelContextProtocol.AspNetCore;

namespace Intellishelf.Api.Modules;

public static class McpModule
{
    public static void Register(IServiceCollection services)
    {
        // Register MCP tools
        services.AddTransient<GetAllBooksTool>();
        services.AddTransient<GetBooksByAuthorTool>();

        // Register MCP tools service for ChatService integration
        services.AddTransient<IMcpToolsService, McpToolsService>();

        // Register MCP server with HTTP transport
        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();
    }

    public static void MapMcpEndpoints(WebApplication app)
    {
        // Map MCP endpoints at /mcp
        app.MapMcp("/mcp");
    }
}
