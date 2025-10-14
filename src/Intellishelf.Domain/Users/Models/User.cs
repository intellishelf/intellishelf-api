namespace Intellishelf.Domain.Users.Models;

public record User(
    string Id, 
    string Email, 
    string? PasswordHash, 
    string? PasswordSalt,
    AuthProvider AuthProvider,
    string? ExternalId
);
