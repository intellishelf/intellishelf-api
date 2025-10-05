using Intellishelf.Api.Configuration;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Intellishelf.Api.Modules;

public static class DbModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        var dbSections = builder
            .Configuration
            .GetSection(DatabaseConfig.SectionName);

        builder.Services.Configure<DatabaseConfig>(dbSections);

        var dbOptions = dbSections
            .Get<DatabaseConfig>() ?? throw new InvalidOperationException("Database configuration is missing");

        builder.Services.AddSingleton<IMongoDatabase>(x =>
        {
            var mongoClient = x.GetRequiredService<MongoClient>();
            return mongoClient.GetDatabase(dbOptions.DatabaseName);
        });

        var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
        ConventionRegistry.Register("IgnoreExtraElements", conventionPack, _ => true);
    }
}