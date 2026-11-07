using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Ai.Services;
using Intellishelf.Domain.Chat.Services;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class AiExtensions
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidateOnStart<AiConfig>()
            .Bind(configuration.GetRequiredSection(AiConfig.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton(sp =>
            new ChatClient("gpt-4o-mini", sp.GetRequiredService<IOptions<AiConfig>>().Value.OpenAiApiKey));
        services.AddTransient<IAiService, AiService>();
        services.AddTransient<IChatService, ChatService>();

        return services;
    }
}
