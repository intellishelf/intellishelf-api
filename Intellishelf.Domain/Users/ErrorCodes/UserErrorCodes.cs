namespace Intellishelf.Domain.Users.ErrorCodes;

public static class UserErrorCodes
{
    public const string UserNotFound = "User.NotFound";
    public const string UserAlreadyExists = "User.AlreadyExists";
    public const string InvalidCredentials = "User.CantLogin";
}