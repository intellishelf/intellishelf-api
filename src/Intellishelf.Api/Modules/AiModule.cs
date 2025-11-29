using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Ai.Services;
using Intellishelf.Domain.Chat.Services;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Intellishelf.Api.Modules;

public static class AiModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        var aiSection = builder.Configuration.GetSection(AiConfig.SectionName);

        builder.Services.Configure<AiConfig>(aiSection);

        var aiConfig = aiSection
            .Get<AiConfig>() ?? throw new InvalidOperationException("AI configuration is missing");

        builder.Services.AddSingleton(_ => new ChatClient("gpt-4o-mini", aiConfig.OpenAiApiKey));
        builder.Services.AddSingleton(_ => new EmbeddingClient("text-embedding-3-large", aiConfig.OpenAiApiKey));
        builder.Services.AddTransient<IAiService, AiService>();
        builder.Services.AddTransient<IChatService, ChatService>();
    }
}