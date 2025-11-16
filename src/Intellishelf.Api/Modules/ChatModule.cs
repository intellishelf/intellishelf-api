using Intellishelf.Domain.Chat.Services;

namespace Intellishelf.Api.Modules;

public static class ChatModule
{
    public static void Register(IServiceCollection services)
    {
        services.AddTransient<IChatService, ChatService>();
    }
}
