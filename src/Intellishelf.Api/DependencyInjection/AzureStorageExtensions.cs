using Azure.Storage.Blobs;
using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Files.Services;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class AzureStorageExtensions
{
    public static IServiceCollection AddAzureStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidateOnStart<AzureConfig>()
            .Bind(configuration.GetRequiredSection(AzureConfig.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AzureConfig>>().Value;
            return new BlobContainerClient(
                config.StorageConnectionString,
                config.StorageContainer,
                new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_11_05));
        });

        services.AddScoped<IFileStorageService, FileStorageService>();

        return services;
    }
}
