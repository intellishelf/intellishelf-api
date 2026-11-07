using System.Runtime.CompilerServices;

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

app.UseApiPipeline();
app.MapMcpEndpoints();
app.Run();
