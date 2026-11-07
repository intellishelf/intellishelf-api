using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Intellishelf.Unit.Tests.DependencyInjection;

internal static class DiTestConfig
{
    public const string AuthKey = "synthetic-signing-key-0123456789-0123456789-abcd";

    public static readonly string[] RequiredKeys =
    [
        "Database:ConnectionString",
        "Database:DatabaseName",
        "Azure:StorageConnectionString",
        "Azure:StorageContainer",
        "Ai:OpenAiApiKey",
        "Auth:Key",
        "Auth:Google:ClientId",
        "Auth:Google:ClientSecret"
    ];

    public static Dictionary<string, string?> Complete() => new()
    {
        ["Database:ConnectionString"] = "mongodb://localhost:27017",
        ["Database:DatabaseName"] = "synthetic-db",
        ["Azure:StorageConnectionString"] = "UseDevelopmentStorage=true",
        ["Azure:StorageContainer"] = "synthetic-container",
        ["Ai:OpenAiApiKey"] = "synthetic-openai-key",
        ["Auth:Key"] = AuthKey,
        ["Auth:Google:ClientId"] = "synthetic-google-client-id",
        ["Auth:Google:ClientSecret"] = "synthetic-google-client-secret"
    };

    public static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    public static IServiceCollection AddConfigured(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddDatabase(configuration)
            .AddAzureStorage(configuration)
            .AddUsers(configuration)
            .AddAi(configuration);

        return services;
    }
}
