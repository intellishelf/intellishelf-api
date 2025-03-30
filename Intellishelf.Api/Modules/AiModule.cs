using Intellishelf.Api.Configuration;
using Intellishelf.Api.Services;
using Intellishelf.Domain.Ai.Services;
using OpenAI.Chat;

namespace Intellishelf.Api.Modules;

public static class AiModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AiConfig>(
            builder.Configuration.GetSection(AiConfig.SectionName));

        var aiConfig = builder.Configuration
            .GetSection(AiConfig.SectionName)
            .Get<AiConfig>() ?? throw new InvalidOperationException("AI configuration is missing");

        builder.Services.AddSingleton<ChatClient>(_ => new ChatClient("gpt-4o-mini", aiConfig.OpenAiApiKey));
        builder.Services.AddHttpClient<AiServiceOld>();
        builder.Services.AddTransient<IAiService, AiService>();
        builder.Services.AddTransient<IAiService, AiService>();
    }
}