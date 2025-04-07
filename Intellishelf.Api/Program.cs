using Intellishelf.Api.Modules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
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
UsersModule.Register(builder);
BooksModule.Register(builder.Services);
AiModule.Register(builder);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevPolicy");
}

app.UsePathBase(new PathString("/api"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();