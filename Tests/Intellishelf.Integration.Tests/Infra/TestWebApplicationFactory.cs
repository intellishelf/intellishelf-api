using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Users.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Intellishelf.Integration.Tests.Infra;

internal class TestWebApplicationFactory(MongoDbFixture mongoFixture) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder
            .ConfigureHostConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{DatabaseConfig.SectionName}:ConnectionString"] = mongoFixture.ConnectionString,
                    [$"{DatabaseConfig.SectionName}:DatabaseName"] = mongoFixture.Database.DatabaseNamespace.DatabaseName,
                    [$"{AuthConfig.SectionName}:Google:ClientId"] = "fake",
                    [$"{AuthConfig.SectionName}:Google:ClientSecret"] = "fake",
                    [$"{AiConfig.SectionName}:OpenAiApiKey"] = "fake",
                    [$"{AzureConfig.SectionName}:StorageConnectionString"] = "name=value",
                    [$"{AzureConfig.SectionName}:StorageContainer"] = "fake"
                });
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