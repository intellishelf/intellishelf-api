using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Ai.Services;

namespace Intellishelf.Api.Modules;

public static class AiModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AiConfig>(
            builder.Configuration.GetSection(AiConfig.SectionName));

        builder.Services.AddTransient<IAiService, AiService>();
    }
}