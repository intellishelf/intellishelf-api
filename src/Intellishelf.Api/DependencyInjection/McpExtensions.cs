using Intellishelf.Api.Mcp.Tools;
using Intellishelf.Api.Services;
using Intellishelf.Domain.Chat.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class McpExtensions
{
    public static IServiceCollection AddMcp(this IServiceCollection services)
    {
        services.AddTransient<GetAllBooksTool>();
        services.AddTransient<GetBooksByAuthorTool>();

        // ChatService integration
        services.AddTransient<IMcpToolsService, McpToolsService>();

        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();

        return services;
    }

    public static WebApplication MapMcpEndpoints(this WebApplication app)
    {
        app.MapMcp("/mcp");

        return app;
    }
}
