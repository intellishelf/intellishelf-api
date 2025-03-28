using Microsoft.IdentityModel.Tokens;
using System.Text;
using Intellishelf.Api.Configuration;
using Intellishelf.Api.Services;
using Intellishelf.Data.Auth.DataAccess;
using Intellishelf.Data.Books.DataAccess;
using Intellishelf.Domain.Auth.DataAccess;
using Intellishelf.Domain.Auth.Services;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Services;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Intellishelf.Domain.Auth.Config;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<DatabaseConfig>(
    builder.Configuration.GetSection(DatabaseConfig.SectionName));
builder.Services.Configure<AuthConfig>(
    builder.Configuration.GetSection(AuthConfig.SectionName));
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure MongoDB with options
var dbOptions = builder.Configuration
    .GetSection(DatabaseConfig.SectionName)
    .Get<DatabaseConfig>() ?? throw new InvalidOperationException("Database configuration is missing");

var mongoClient = new MongoClient(dbOptions.ConnectionString);
var mongoDatabase = mongoClient.GetDatabase(dbOptions.DatabaseName);
builder.Services.AddSingleton(mongoDatabase);
var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
ConventionRegistry.Register("IgnoreExtraElements", conventionPack, _ => true);
// Register HttpClient for AI integration
builder.Services.AddHttpClient<AiService>();
builder.Services.AddHttpContextAccessor();

// Register custom services
builder.Services.AddTransient<IUserDao, UserDao>();
builder.Services.AddTransient<IBookDao, BookDao>();
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IBookService, BookService>();
builder.Services.AddTransient<IUserContext, UserContext>();

var jwtOptions = builder.Configuration
    .GetSection(AuthConfig.SectionName)
    .Get<AuthConfig>() ?? throw new InvalidOperationException("Auth configuration is missing");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = AuthConfig.Scheme;
    options.DefaultChallengeScheme = AuthConfig.Scheme;
})
.AddJwtBearer(AuthConfig.Scheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtOptions.Key)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
    };
});

var app = builder.Build();

// Configure middleware pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UsePathBase(new PathString("/api"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();