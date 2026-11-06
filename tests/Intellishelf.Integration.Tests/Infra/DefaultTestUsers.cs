using System.Security.Claims;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Users.Helpers;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Integration.Tests.Infra;

internal static class DefaultTestUsers
{
    internal static readonly TestUser Authenticated = TestUser.Create(
        id: "67ce0050034e9ade4072526d",
        email: "user@test.com",
        password: "SecurePassword123!",
        provider: AuthProvider.Email);

    internal sealed record TestUser(
        string Id,
        string Email,
        string Password,
        AuthProvider Provider,
        string PasswordHash,
        string PasswordSalt)
    {
        public static TestUser Create(string id, string email, string password, AuthProvider provider)
        {
            var (hash, salt) = AuthHelper.CreatePasswordHash(password);
            return new TestUser(id, email, password, provider, hash, salt);
        }

        public Claim[] ToClaims() => new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Id),
            new Claim(ClaimTypes.Email, Email)
        };

        public UserEntity ToEntity() => new()
        {
            Id = Id,
            Email = Email,
            AuthProvider = Provider,
            PasswordHash = PasswordHash,
            PasswordSalt = PasswordSalt
        };
    }
}
