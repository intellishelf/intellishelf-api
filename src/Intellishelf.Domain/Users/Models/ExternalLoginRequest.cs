namespace Intellishelf.Domain.Users.Models;

public record ExternalLoginRequest(string Email, string ExternalId, AuthProvider Provider);
