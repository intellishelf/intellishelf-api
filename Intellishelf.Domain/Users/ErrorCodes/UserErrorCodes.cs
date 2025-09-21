namespace Intellishelf.Domain.Users.ErrorCodes;

public static class UserErrorCodes
{
    public const string OAuthError = "User.OAuthError";
    public const string UserNotFound = "User.NotFound";
    public const string AlreadyExists = "User.AlreadyExists";
    public const string Unauthorized = "User.Unauthorized";
    public const string RefreshTokenNotFound = "User.RefreshToken.NotFound";
    public const string RefreshTokenExpired = "User.RefreshToken.Expired";
    public const string RefreshTokenRevoked = "User.RefreshToken.Revoked";
}