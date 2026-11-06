using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Intellishelf.Integration.Tests.Infra;

internal class TestWebApplicationFactory(MongoDbFixture mongoFixture, AzuriteFixture azuriteFixture) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder
            .ConfigureHostConfiguration((cfg) =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    [$"{DatabaseConfig.SectionName}:ConnectionString"] = mongoFixture.ConnectionString,
                    [$"{DatabaseConfig.SectionName}:DatabaseName"] = mongoFixture.Database.DatabaseNamespace.DatabaseName,
                    [$"{AuthConfig.SectionName}:Google:ClientId"] = "fake",
                    [$"{AuthConfig.SectionName}:Google:ClientSecret"] = "fake",
                    [$"{AiConfig.SectionName}:OpenAiApiKey"] = "fake",
                    [$"{AzureConfig.SectionName}:StorageConnectionString"] = azuriteFixture.ConnectionString,
                    [$"{AzureConfig.SectionName}:StorageContainer"] = TestConstants.StorageContainerName,
                    [$"{AuthConfig.SectionName}:Key"] = "1234567890123456789012345678901234567890"
                };
                cfg.AddInMemoryCollection(testConfig);
            })
            .ConfigureServices(services =>
            {
                services.RemoveAll<IMongoDatabase>();
                services.AddSingleton(mongoFixture.Database);

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });

        return base.CreateHost(builder);
    }
}