namespace Intellishelf.Domain.Users.Config;

public class AuthConfig
{
    public const string SectionName = "Auth";
    public const string JwtScheme = "JwtBearer";
    public const string CookieScheme = "Cookies";

    public string RefreshTokenCookieName { get; init; } = "refresh_token";

    public string AuthCookieName { get; init; } = "auth";

    public required string Key { get; init; }

    public required int AuthExpirationMinutes { get; init; } = 30;
    public required int RefreshTokenExpirationDays { get; init; } = 7;

    public required GoogleAuthConfig Google { get; init; }
}

public class GoogleAuthConfig
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}