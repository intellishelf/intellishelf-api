using System.Runtime.CompilerServices;
using Intellishelf.Api.Modules;

[assembly: InternalsVisibleTo("Intellishelf.Integration.Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalhostPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost"))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

DbModule.Register(builder);
AzureModule.Register(builder);
UsersModule.Register(builder);
BooksModule.Register(builder.Services);
AiModule.Register(builder);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalhostPolicy");
}

app.UsePathBase(new PathString("/api"));
app.UseExceptionHandler();
app.UseAuthentication();
app.UseStatusCodePages();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();