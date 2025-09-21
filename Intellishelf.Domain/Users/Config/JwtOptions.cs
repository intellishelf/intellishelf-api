namespace Intellishelf.Domain.Users.Config;

public class AuthConfig
{
    public const string SectionName = "Auth";
    public const string Scheme = "JwtBearer";
    public const string ExternalCookieScheme = "External";

    // Access token settings
    public int AccessTokenExpirationMinutes { get; init; } = 30;

    // Refresh token settings
    public int RefreshTokenExpirationDays { get; init; } = 7;

    public string RefreshTokenCookieName { get; init; } = "__intellishelf_refresh";
    public bool RefreshTokenCookieSecure { get; init; } = true;
    public string? RefreshTokenCookieDomain { get; init; }
        = null;

    public string ExternalCookieName { get; init; } = "__intellishelf_external";
    public int ExternalCookieLifetimeMinutes { get; init; } = 5;

    public required string Key { get; init; }

    public required GoogleAuthConfig Google { get; init; }
    public required FacebookAuthConfig Facebook { get; init; }
}

public class GoogleAuthConfig
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}

public class FacebookAuthConfig
{
    public required string AppId { get; init; }
    public required string AppSecret { get; init; }
}