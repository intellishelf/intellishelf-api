namespace Intellishelf.Domain.Users.Models;

public record RefreshToken(
    string Id,
    string Token,
    string UserId,
    DateTime ExpiryDate,
    bool IsRevoked,
    DateTime CreatedAt);
