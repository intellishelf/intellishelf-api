using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Ai.Services;
using OpenAI.Chat;

namespace Intellishelf.Api.Modules;

public static class AiModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        var aiSection = builder.Configuration.GetSection(AiConfig.SectionName);

        builder.Services.Configure<AiConfig>(aiSection);

        var aiConfig = aiSection
            .Get<AiConfig>() ?? throw new InvalidOperationException("AI configuration is missing");

        builder.Services.AddSingleton(_ => new ChatClient("gpt-4o", aiConfig.OpenAiApiKey));
        builder.Services.AddTransient<IAiService, AiService>();
    }
}