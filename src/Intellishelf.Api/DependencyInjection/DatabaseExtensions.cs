using Intellishelf.Api.Configuration;
using Intellishelf.Data.Indexes;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidateOnStart<DatabaseConfig>()
            .Bind(configuration.GetRequiredSection(DatabaseConfig.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<DatabaseConfig>>().Value;
            return new MongoClient(config.ConnectionString).GetDatabase(config.DatabaseName);
        });

        return services;
    }

    public static Task InitializeDatabaseAsync(this WebApplication app) =>
        MongoIndexSetup.EnsureAsync(app.Services.GetRequiredService<IMongoDatabase>());

}
