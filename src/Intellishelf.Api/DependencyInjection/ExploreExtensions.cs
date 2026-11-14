using Intellishelf.Api.Configuration;
using Intellishelf.Data.Books.DataAccess;
using Intellishelf.Domain.Explore;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace Microsoft.Extensions.DependencyInjection;

public static class ExploreExtensions
{
    public static IServiceCollection AddExplore(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptionsWithValidateOnStart<ExploreOptions>()
            .Bind(configuration.GetSection(ExploreOptions.SectionName))
            .Validate(o => o.IsValid(), "Invalid Explore configuration")
            .Validate(o => o.Provider != "StandIn" || environment.IsDevelopment() || environment.IsEnvironment("Test"), "Stand-in provider is not allowed in this environment");
        services.AddSingleton<IEmbeddingProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ExploreOptions>>();
            if (options.Value.Provider == "StandIn") return new StandInEmbeddingProvider(options);
            var key = sp.GetRequiredService<IOptions<AiConfig>>().Value.OpenAiApiKey;
            return new OpenAiEmbeddingProvider(options, new EmbeddingClient(options.Value.Model, key));
        });
        services.AddTransient<BookIndexer>();
        services.AddTransient<ExploreService>();
        services.AddTransient<IExploreDao, ExploreDao>();
        return services;
    }
}
