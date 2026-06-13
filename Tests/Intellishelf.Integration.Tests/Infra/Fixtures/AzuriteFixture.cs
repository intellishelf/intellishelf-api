using Azure.Storage.Blobs;
using Testcontainers.Azurite;
using Xunit;

namespace Intellishelf.Integration.Tests.Infra.Fixtures;

public class AzuriteFixture : IAsyncLifetime
{
    private AzuriteContainer? _azuriteContainer;
    public string ConnectionString => _azuriteContainer?.GetConnectionString()
        ?? throw new InvalidOperationException("Azurite fixture has not been initialized.");
    private static readonly BlobClientOptions BlobClientOptions = new(BlobClientOptions.ServiceVersion.V2025_11_05);

    public async ValueTask InitializeAsync()
    {
        _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .Build();

        await _azuriteContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_azuriteContainer is not null)
        {
            await _azuriteContainer.DisposeAsync();
        }
    }

    public async Task<string> SeedBlobAsync(string blobPath, byte[] content)
    {
        var containerClient = new BlobContainerClient(ConnectionString, TestConstants.StorageContainerName, BlobClientOptions);
        await containerClient.CreateIfNotExistsAsync();

        using var stream = new MemoryStream(content);
        var blobClient = containerClient.GetBlobClient(blobPath);
        await blobClient.UploadAsync(stream, overwrite: true);

        return blobClient.Uri.ToString();
    }

    public async Task<bool> BlobExistsAsync(string blobPath)
    {
        var containerClient = new BlobContainerClient(ConnectionString, TestConstants.StorageContainerName, BlobClientOptions);
        var blobClient = containerClient.GetBlobClient(blobPath);
        var exists = await blobClient.ExistsAsync();
        return exists.Value;
    }

    public Task<bool> BlobExistsFromUrlAsync(string blobUrl)
    {
        var builder = new BlobUriBuilder(new Uri(blobUrl));
        return BlobExistsAsync(builder.BlobName);
    }
}
