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
}