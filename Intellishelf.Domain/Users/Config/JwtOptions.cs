namespace Intellishelf.Domain.Users.Config;

public class AuthConfig
{
    public const string SectionName = "Auth";


    public const string Scheme = "JwtBearer";
    public const int ExpirationDays = 30;
    public required string Key { get; init; }
}