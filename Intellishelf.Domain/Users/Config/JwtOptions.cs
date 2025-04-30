namespace Intellishelf.Domain.Users.Config;

public class AuthConfig
{
    public const string SectionName = "Auth";
    public const string Scheme = "JwtBearer";
    
    // Access token settings
    public int AccessTokenExpirationMinutes { get; init; } = 30;
    
    // Refresh token settings
    public int RefreshTokenExpirationDays { get; init; } = 7;
    
    public required string Key { get; init; }
}
