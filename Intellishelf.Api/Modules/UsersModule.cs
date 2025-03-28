using System.Text;
using Intellishelf.Api.Mappers.Auth;
using Intellishelf.Data.Auth.DataAccess;
using Intellishelf.Domain.Auth.Config;
using Intellishelf.Domain.Auth.DataAccess;
using Intellishelf.Domain.Auth.Services;
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtOptions.Key)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                };
            });

        builder.Services.AddSingleton<IAuthMapper, AuthMapper>();
        builder.Services.AddTransient<IUserDao, UserDao>();
        builder.Services.AddTransient<IAuthService, AuthService>();
    }
}