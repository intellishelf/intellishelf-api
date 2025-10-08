using Azure.Storage.Blobs;
using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Files.Services;

namespace Intellishelf.Api.Modules;

public static class AzureModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        var azureSection = builder.Configuration.GetSection(AzureConfig.SectionName);

        builder.Services.Configure<AzureConfig>(azureSection);

        var azureConfig = azureSection
            .Get<AzureConfig>() ?? throw new InvalidOperationException("Azure  configuration is missing");

        builder.Services.AddSingleton(_ =>
            new BlobContainerClient(azureConfig.StorageConnectionString, azureConfig.StorageContainer));

        builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    }
}