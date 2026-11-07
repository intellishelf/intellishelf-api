using System.Text;
using Intellishelf.Domain.Users.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using OpenAI.Chat;
using Azure.Storage.Blobs;
using Xunit;

namespace Intellishelf.Unit.Tests.DependencyInjection;

public class RegistrationTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?>? config = null) =>
        DiTestConfig.AddConfigured(DiTestConfig.Build(config ?? DiTestConfig.Complete())).BuildServiceProvider();

    [Fact]
    public void GivenAuthRegistered_WhenReadingAuthenticationOptions_ThenCustomIsDefaultAndChallengeScheme()
    {
        using var provider = BuildProvider();

        var options = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        Assert.Equal("Custom", options.DefaultScheme);
        Assert.Equal("Custom", options.DefaultChallengeScheme);
    }

    [Theory]
    [InlineData("Bearer synthetic-token", AuthConfig.JwtScheme)]
    [InlineData(null, AuthConfig.CookieScheme)]
    public void GivenCustomPolicyScheme_WhenSelectingForRequest_ThenForwardsByAuthorizationHeader(string? header, string expected)
    {
        using var provider = BuildProvider();
        var selector = provider.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>().Get("Custom").ForwardDefaultSelector!;
        var context = new DefaultHttpContext();

        if (header is not null)
        {
            context.Request.Headers.Authorization = header;
        }

        Assert.Equal(expected, selector(context));
    }

    [Fact]
    public void GivenAuthRegistered_WhenReadingJwtBearerOptions_ThenValidationParametersMatchConfiguredKey()
    {
        using var provider = BuildProvider();

        var parameters = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(AuthConfig.JwtScheme).TokenValidationParameters;

        Assert.True(parameters.ValidateIssuerSigningKey);
        var key = Assert.IsType<SymmetricSecurityKey>(parameters.IssuerSigningKey);
        Assert.Equal(Encoding.UTF8.GetBytes(DiTestConfig.AuthKey), key.Key);
        Assert.False(parameters.ValidateIssuer);
        Assert.False(parameters.ValidateAudience);
        Assert.True(parameters.ValidateLifetime);
        Assert.Equal(TimeSpan.Zero, parameters.ClockSkew);
    }

    [Fact]
    public void GivenAuthRegisteredWithDefaults_WhenReadingCookieOptions_ThenMatchesBaseSettings()
    {
        using var provider = BuildProvider();

        var options = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(AuthConfig.CookieScheme);

        Assert.Equal("auth", options.Cookie.Name);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.Equal("/", options.Cookie.Path);
        Assert.Equal(TimeSpan.FromMinutes(30), options.ExpireTimeSpan);
    }

    [Fact]
    public void GivenCookieNameAndExpiryConfigured_WhenReadingCookieOptions_ThenUsesConfiguredValues()
    {
        var config = DiTestConfig.Complete();
        config["Auth:AuthCookieName"] = "synthetic_cookie";
        config["Auth:AuthExpirationMinutes"] = "45";
        using var provider = BuildProvider(config);

        var options = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(AuthConfig.CookieScheme);

        Assert.Equal("synthetic_cookie", options.Cookie.Name);
        Assert.Equal(TimeSpan.FromMinutes(45), options.ExpireTimeSpan);
    }

    [Fact]
    public void GivenAuthRegistered_WhenReadingGoogleOptions_ThenMatchesBaseSettings()
    {
        var config = DiTestConfig.Complete();
        using var provider = BuildProvider(config);

        var options = provider.GetRequiredService<IOptionsMonitor<GoogleOptions>>().Get(GoogleDefaults.AuthenticationScheme);

        Assert.Equal(config["Auth:Google:ClientId"], options.ClientId);
        Assert.Equal(config["Auth:Google:ClientSecret"], options.ClientSecret);
        Assert.False(options.SaveTokens);
        Assert.Equal(AuthConfig.CookieScheme, options.SignInScheme);
        Assert.True(options.UsePkce);
    }

    [Fact]
    public void GivenCompleteConfig_WhenResolvingClients_ThenUseConfiguredNames()
    {
        var config = DiTestConfig.Complete();
        using var provider = BuildProvider(config);

        Assert.Equal(config["Database:DatabaseName"], provider.GetRequiredService<IMongoDatabase>().DatabaseNamespace.DatabaseName);
        Assert.Equal(config["Azure:StorageContainer"], provider.GetRequiredService<BlobContainerClient>().Name);
        Assert.NotNull(provider.GetRequiredService<ChatClient>());
    }
}
