namespace Intellishelf.Domain.Users.Models;

public record NewUser(string Email, string PasswordHash, string PasswordSalt);