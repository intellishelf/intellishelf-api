namespace Intellishelf.Domain.Users.Models;

public record LoginResult(
    string UserId,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry);