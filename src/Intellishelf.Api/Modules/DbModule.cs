using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Books.DataAccess;
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

        builder.Services.AddSingleton<IMongoDatabase>(_ => new MongoClient(dbOptions.ConnectionString).GetDatabase(dbOptions.DatabaseName));
    }

    /// <summary>
    /// Ensures MongoDB indexes are created at startup.
    /// </summary>
    public static async Task EnsureIndexesAsync(IServiceProvider services)
    {
        var bookDao = services.GetRequiredService<IBookDao>();
        await bookDao.EnsureIndexesAsync();
    }
}