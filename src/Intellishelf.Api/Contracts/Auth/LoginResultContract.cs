namespace Intellishelf.Api.Contracts.Auth;

public record LoginResultContract(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry);
