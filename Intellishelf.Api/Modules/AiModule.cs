using Intellishelf.Api.Configuration;
using Intellishelf.Api.Services;
using Intellishelf.Domain.Ai.Services;

namespace Intellishelf.Api.Modules;

public static class AiModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AiConfig>(
            builder.Configuration.GetSection(AiConfig.SectionName));

        builder.Services.AddHttpClient<AiServiceOld>();
        builder.Services.AddTransient<IAiService, AiService>();
    }
}