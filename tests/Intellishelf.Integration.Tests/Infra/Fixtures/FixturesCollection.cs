using Xunit;

namespace Intellishelf.Integration.Tests.Infra.Fixtures;

[CollectionDefinition("Integration Tests")]
public class FixturesCollection : ICollectionFixture<MongoDbFixture>, ICollectionFixture<AzuriteFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}