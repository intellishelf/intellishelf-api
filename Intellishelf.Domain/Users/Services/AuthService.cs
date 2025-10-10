using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.Helpers;
using Intellishelf.Domain.Users.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Intellishelf.Domain.Users.Services;

public class AuthService(
    IOptions<AuthConfig> options,
    IUserDao userDao,
    IRefreshTokenDao refreshTokenDao)
    : IAuthService
{
    public async Task<TryResult<User>> TryFindByIdAsync(string id) =>
        await userDao.TryFindByIdAsync(id);

    public async Task<TryResult<LoginResult>> TryRegisterAsync(string email, string password)
    {
        var existingUserResult = await userDao.TryUserExists(email);

        if (!existingUserResult.IsSuccess)
            return existingUserResult.Error;

        if (existingUserResult.Value)
            return new Error(UserErrorCodes.AlreadyExists, $"User with email {email} already exists.");

        var (passwordHash, passwordSalt) = AuthHelper.CreatePasswordHash(password);

        var result = await userDao.TryAdd(new NewUser(
            Email: email,
            PasswordHash: passwordHash,
            PasswordSalt: passwordSalt,
            AuthProvider: AuthProvider.Email));

        if (!result.IsSuccess)
            return result.Error;

        return await GenerateTokensAsync(result.Value);
    }

    public async Task<TryResult<LoginResult>> TrySignInAsync(LoginRequest request)
    {
        var result = await userDao.TryFindByEmailAsync(request.Email);

        if (!result.IsSuccess)
            return result.Error;

        var user = result.Value;

        if (user.AuthProvider != AuthProvider.Email)
            return new Error(UserErrorCodes.Unauthorized, $"Please sign in with {user.AuthProvider}");

        if (user.PasswordHash == null || user.PasswordSalt == null)
            return new Error(UserErrorCodes.Unauthorized, "Invalid credentials.");

        var verifyHashResult = AuthHelper.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt);

        if (!verifyHashResult)
            return new Error(UserErrorCodes.Unauthorized, "Invalid credentials.");

        return await GenerateTokensAsync(user);
    }

    public async Task<TryResult<LoginResult>> TrySignInExternalAsync(ExternalLoginRequest request)
    {
        var (email, externalId, provider) = request;

        var userExistsResult = await userDao.TryUserExists(email);

        if (userExistsResult.IsSuccess)
        {

            if (userExistsResult.Value)
            {
                var existingUserResult = await userDao.TryFindByEmailAsync(email);

                var user = existingUserResult.Value!;

                if (user.AuthProvider != provider)
                    return new Error(UserErrorCodes.Unauthorized, $"Please sign in with {user.AuthProvider}");

                if (user.ExternalId != externalId)
                    return new Error(UserErrorCodes.Unauthorized, "Invalid OAuth credentials");

                return await GenerateTokensAsync(user);
            }

            var newUser = new NewUser(
                Email: email,
                PasswordHash: null,
                PasswordSalt: null,
                AuthProvider: provider,
                ExternalId: externalId);

            var createResult = await userDao.TryAdd(newUser);

            if (!createResult.IsSuccess)
                return createResult.Error;

            return await GenerateTokensAsync(createResult.Value);
        }

        return userExistsResult.Error;
    }

    public async Task<TryResult<LoginResult>> TryRefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshTokenResult = await refreshTokenDao.TryFindByTokenAsync(request.RefreshToken);

        if (!refreshTokenResult.IsSuccess)
            return refreshTokenResult.Error;

        var refreshToken = refreshTokenResult.Value;

        if (refreshToken.ExpiryDate < DateTime.UtcNow)
            return new Error(UserErrorCodes.RefreshTokenExpired, "Refresh token has expired");

        if (refreshToken.IsRevoked)
            return new Error(UserErrorCodes.RefreshTokenRevoked, "Refresh token has been revoked");

        var userResult = await userDao.TryFindByIdAsync(refreshToken.UserId);

        if (!userResult.IsSuccess)
            return userResult.Error;

        var revokeResult = await refreshTokenDao.TryUpdateAsync(refreshToken with { IsRevoked = true });

        if (!revokeResult.IsSuccess)
            return revokeResult.Error;

        return await GenerateTokensAsync(userResult.Value);
    }

    public async Task<TryResult<bool>> TryRevokeRefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshTokenResult = await refreshTokenDao.TryFindByTokenAsync(request.RefreshToken);

        if (!refreshTokenResult.IsSuccess)
            return refreshTokenResult.Error;

        var refreshToken = refreshTokenResult.Value;
        return await refreshTokenDao.TryUpdateAsync(refreshToken with { IsRevoked = true });
    }

    private async Task<TryResult<LoginResult>> GenerateTokensAsync(User user)
    {
        var refreshToken = GenerateRefreshToken(user.Id);
        var addResult = await refreshTokenDao.TryAddAsync(refreshToken);

        if (!addResult.IsSuccess)
            return addResult.Error;

        var persistedRefreshToken = addResult.Value;
        var accessToken = GenerateAccessToken(user);
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(options.Value.AuthExpirationMinutes);

        return new LoginResult(user.Id, accessToken, persistedRefreshToken.Token, accessTokenExpiry, persistedRefreshToken.ExpiryDate);
    }

    private string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var keyBytes = Encoding.UTF8.GetBytes(options.Value.Key);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Email)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256
        );
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(options.Value.AuthExpirationMinutes),
            SigningCredentials = credentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(string userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new RefreshToken(
            Id: string.Empty,
            Token: Convert.ToBase64String(randomBytes),
            UserId: userId,
            ExpiryDate: DateTime.UtcNow.AddDays(options.Value.RefreshTokenExpirationDays),
            IsRevoked: false,
            CreatedAt: DateTime.UtcNow
        );
    }
}