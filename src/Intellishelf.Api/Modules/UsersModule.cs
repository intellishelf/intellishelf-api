using System.Text;
using Intellishelf.Api.Services;
using Intellishelf.Data.Users.DataAccess;
using Intellishelf.Data.Users.Mappers;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;

namespace Intellishelf.Api.Modules;

public static class UsersModule
{
    public static void Register(IHostApplicationBuilder builder)
    {
        var authSection = builder.Configuration.GetSection(AuthConfig.SectionName);

        builder.Services.Configure<AuthConfig>(authSection);

        var authConfig = authSection
            .Get<AuthConfig>() ?? throw new InvalidOperationException("Auth configuration is missing");

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "Custom";
                options.DefaultChallengeScheme = "Custom";
            })
            .AddPolicyScheme("Custom", "JWT or Cookie", options =>
            {
                options.ForwardDefaultSelector = ctx =>
                {
                    var auth = ctx.Request.Headers.Authorization.ToString();
                    return !string.IsNullOrEmpty(auth) ? AuthConfig.JwtScheme : AuthConfig.CookieScheme;
                };
            })
            .AddJwtBearer(AuthConfig.JwtScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfig.Key)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew to ensure tokens expire exactly when they should
                };
            })
            .AddCookie(AuthConfig.CookieScheme, options =>
            {
                options.Cookie.Name = authConfig.AuthCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(authConfig.AuthExpirationMinutes);
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = authConfig.Google.ClientId;
                options.ClientSecret = authConfig.Google.ClientSecret;
                options.SaveTokens = false;
                options.SignInScheme = AuthConfig.CookieScheme;
                options.UsePkce = true;
            });

        // Mappers
        builder.Services.AddSingleton<IUserMapper, UserMapper>();
        builder.Services.AddSingleton<Mappers.Users.IUserMapper, Mappers.Users.UserMapper>();
        builder.Services.AddSingleton<IRefreshTokenMapper, RefreshTokenMapper>();
        
        // Data Access
        builder.Services.AddTransient<IUserDao, UserDao>();
        builder.Services.AddTransient<IRefreshTokenDao, RefreshTokenDao>();
        
        // Services
        builder.Services.AddHttpClient();
        builder.Services.AddTransient<IAuthService, AuthService>();

        // Background Services
        builder.Services.AddHostedService<RefreshTokenCleanupService>();
    }
}