using System.Runtime.CompilerServices;
using Intellishelf.Data.Indexes;
using MongoDB.Driver;

[assembly: InternalsVisibleTo("Intellishelf.Integration.Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiDefaults()
    .AddDatabase(builder.Configuration)
    .AddAzureStorage(builder.Configuration)
    .AddUsers(builder.Configuration)
    .AddBooks()
    .AddAi(builder.Configuration)
    .AddMcp();

var app = builder.Build();

await MongoIndexSetup.EnsureAsync(app.Services.GetRequiredService<IMongoDatabase>(), updateExisting: false);

app.UseApiPipeline();
app.MapMcpEndpoints();
app.Run();
