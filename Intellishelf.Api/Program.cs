using Intellishelf.Api.Modules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
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
    app.UseCors("DevPolicy");
}

app.UsePathBase(new PathString("/api"));
app.UseExceptionHandler();
app.UseAuthentication();
app.UseStatusCodePages();
app.UseAuthorization();
app.MapControllers();
app.Run();