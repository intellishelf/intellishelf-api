using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Intellishelf.Integration.Tests.Infra;

public class MongoDbFixture : IAsyncLifetime
{
    private MongoDbContainer _mongoContainer;
    public string ConnectionString => _mongoContainer.GetConnectionString();
    public IMongoDatabase Database { get; private set; }

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();

        await _mongoContainer.StartAsync();

        var client = new MongoClient(_mongoContainer.GetConnectionString());
        Database = client.GetDatabase("intellishelf-test");
    }

    public async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }
}