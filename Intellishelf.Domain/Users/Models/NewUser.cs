namespace Intellishelf.Domain.Users.Models;

public record NewUser(
    string Email,
    string? PasswordHash,
    string? PasswordSalt,
    AuthProvider AuthProvider,
    string? ExternalId = null
);
