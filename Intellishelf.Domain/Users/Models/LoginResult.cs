namespace Intellishelf.Domain.Users.Models;

public record LoginResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry);
