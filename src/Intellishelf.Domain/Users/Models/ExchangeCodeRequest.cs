namespace Intellishelf.Domain.Users.Models;

public record ExchangeCodeRequest(string Code, string RedirectUri);
