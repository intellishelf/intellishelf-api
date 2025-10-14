namespace Intellishelf.Api.Contracts.Auth;

public record ExternalLoginResultContract(string AccessToken, DateTime AccessTokenExpiry);
