using System.Text;
using Intellishelf.Api.Services;
using Intellishelf.Data.Users.DataAccess;
using Intellishelf.Data.Users.Mappers;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.Services;
using Microsoft.IdentityModel.Tokens;

namespace Intellishelf.Api.Modules;

public static class UsersModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AuthConfig>(
            builder.Configuration.GetSection(AuthConfig.SectionName));

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew to ensure tokens expire exactly when they should
                };
            });

        // Mappers
        builder.Services.AddSingleton<Data.Users.Mappers.IUserMapper, Data.Users.Mappers.UserMapper>();
        builder.Services.AddSingleton<Mappers.Users.IUserMapper, Mappers.Users.UserMapper>();
        builder.Services.AddSingleton<IRefreshTokenMapper, RefreshTokenMapper>();
        
        // Data Access
        builder.Services.AddTransient<IUserDao, UserDao>();
        builder.Services.AddTransient<IRefreshTokenDao, RefreshTokenDao>();
        
        // Services
        builder.Services.AddTransient<IAuthService, AuthService>();
        
        // Background Services
        builder.Services.AddHostedService<RefreshTokenCleanupService>();
    }
}
