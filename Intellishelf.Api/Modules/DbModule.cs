using Intellishelf.Api.Configuration;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Intellishelf.Api.Modules;

public static class DbModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<DatabaseConfig>(
            builder.Configuration.GetSection(DatabaseConfig.SectionName));

        var dbOptions = builder.Configuration
            .GetSection(DatabaseConfig.SectionName)
            .Get<DatabaseConfig>() ?? throw new InvalidOperationException("Database configuration is missing");

        var mongoClient = new MongoClient(dbOptions.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(dbOptions.DatabaseName);

        builder.Services.AddSingleton(mongoDatabase);

        var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
        ConventionRegistry.Register("IgnoreExtraElements", conventionPack, _ => true);
    }
}