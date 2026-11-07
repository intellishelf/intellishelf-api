using System.Text;
using Intellishelf.Api.Services;
using Intellishelf.Data.Users.DataAccess;
using Intellishelf.Data.Users.Mappers;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class UsersExtensions
{
    public static IServiceCollection AddUsers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidateOnStart<AuthConfig>()
            .Bind(configuration.GetRequiredSection(AuthConfig.SectionName))
            .ValidateDataAnnotations();

        services
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
            .AddJwtBearer(AuthConfig.JwtScheme, _ => { })
            .AddCookie(AuthConfig.CookieScheme, options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.SaveTokens = false;
                options.SignInScheme = AuthConfig.CookieScheme;
                options.UsePkce = true;
            });

        services.AddOptions<JwtBearerOptions>(AuthConfig.JwtScheme)
            .Configure<IOptions<AuthConfig>>((options, auth) =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.Value.Key)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew to ensure tokens expire exactly when they should
                };
            });

        services.AddOptions<CookieAuthenticationOptions>(AuthConfig.CookieScheme)
            .Configure<IOptions<AuthConfig>>((options, auth) =>
            {
                options.Cookie.Name = auth.Value.AuthCookieName;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(auth.Value.AuthExpirationMinutes);
            });

        services.AddOptions<GoogleOptions>(GoogleDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthConfig>>((options, auth) =>
            {
                options.ClientId = auth.Value.Google.ClientId;
                options.ClientSecret = auth.Value.Google.ClientSecret;
            });

        // Mappers
        services.AddSingleton<IUserMapper, UserMapper>();
        services.AddSingleton<Intellishelf.Api.Mappers.Users.IUserMapper, Intellishelf.Api.Mappers.Users.UserMapper>();
        services.AddSingleton<IRefreshTokenMapper, RefreshTokenMapper>();

        // Data Access
        services.AddTransient<IUserDao, UserDao>();
        services.AddTransient<IRefreshTokenDao, RefreshTokenDao>();

        // Services
        services.AddHttpClient();
        services.AddTransient<IAuthService, AuthService>();

        // Background Services
        services.AddHostedService<RefreshTokenCleanupService>();

        return services;
    }
}
