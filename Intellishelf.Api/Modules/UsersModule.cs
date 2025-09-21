using System.Text;
using Intellishelf.Api.Services;
using Intellishelf.Data.Users.DataAccess;
using Intellishelf.Data.Users.Mappers;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
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

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = AuthConfig.Scheme;
                options.DefaultAuthenticateScheme = AuthConfig.Scheme;
                options.DefaultChallengeScheme = AuthConfig.Scheme;
                options.DefaultSignInScheme = AuthConfig.ExternalCookieScheme;
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
            })
            .AddCookie(AuthConfig.ExternalCookieScheme, options =>
            {
                options.Cookie.Name = jwtOptions.ExternalCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.Path = "/";
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(jwtOptions.ExternalCookieLifetimeMinutes);
                options.SlidingExpiration = false;
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = jwtOptions.Google.ClientId;
                options.ClientSecret = jwtOptions.Google.ClientSecret;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = false;
                options.SignInScheme = AuthConfig.ExternalCookieScheme;
                options.UsePkce = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
            });

        // Mappers
        builder.Services.AddSingleton<Data.Users.Mappers.IUserMapper, Data.Users.Mappers.UserMapper>();
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