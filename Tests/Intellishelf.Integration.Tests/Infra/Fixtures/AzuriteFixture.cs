using Azure.Storage.Blobs;
using Testcontainers.Azurite;
using Xunit;

namespace Intellishelf.Integration.Tests.Infra.Fixtures;

public class AzuriteFixture : IAsyncLifetime
{
    private AzuriteContainer _azuriteContainer;
    public string ConnectionString => _azuriteContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        _azuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .Build();

        await _azuriteContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _azuriteContainer.DisposeAsync();
    }

    public async Task<string> SeedBlobAsync(string blobPath, byte[] content)
    {
        var containerClient = new BlobContainerClient(ConnectionString, TestConstants.StorageContainerName);
        await containerClient.CreateIfNotExistsAsync();

        using var stream = new MemoryStream(content);
        var blobClient = containerClient.GetBlobClient(blobPath);
        await blobClient.UploadAsync(stream, overwrite: true);

        return blobClient.Uri.ToString();
    }

    public async Task<bool> BlobExistsAsync(string blobPath)
    {
        var containerClient = new BlobContainerClient(ConnectionString, TestConstants.StorageContainerName);
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