using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Intellishelf.Domain.Users.Config;

public class AuthConfig
{
    public const string SectionName = "Auth";
    public const string JwtScheme = "JwtBearer";
    public const string CookieScheme = "Cookies";

    public string RefreshTokenCookieName { get; init; } = "refresh_token";

    public string AuthCookieName { get; init; } = "auth";

    [Required]
    public required string Key { get; init; }

    public required int AuthExpirationMinutes { get; init; } = 30;
    public required int RefreshTokenExpirationDays { get; init; } = 7;

    [Required, ValidateObjectMembers]
    public required GoogleAuthConfig Google { get; init; }
}

public class GoogleAuthConfig
{
    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string ClientSecret { get; init; }
}
