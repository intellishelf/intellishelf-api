using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.ErrorCodes;
using Intellishelf.Domain.Users.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Intellishelf.Domain.Users.Services;

public class AuthService(
    IOptions<AuthConfig> options,
    IUserDao userDao,
    IRefreshTokenDao refreshTokenDao,
    //TODO: move this out of domain service
    HttpClient httpClient)
    : IAuthService
{
    public async Task<TryResult<User>> TryFindByIdAsync(string id) =>
        await userDao.TryFindByIdAsync(id);

    public async Task<TryResult<LoginResult>> TryRegisterAsync(RegisterUserRequest request)
    {
        var existingUserResult = await userDao.TryFindByEmailAsync(request.Email);

        if (existingUserResult.IsSuccess || existingUserResult.Error.Code != UserErrorCodes.UserNotFound)
            return new Error(UserErrorCodes.AlreadyExists, $"User with email {request.Email} already exists.");

        CreatePasswordHash(request.Password, out var passwordHash, out var passwordSalt);

        var result = await userDao.TryAdd(new NewUser(
            Email: request.Email,
            PasswordHash: passwordHash,
            PasswordSalt: passwordSalt,
            AuthProvider: AuthProvider.Email));

        if(!result.IsSuccess)
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

        var verifyHashResult = VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt);

        if (!verifyHashResult)
            return new Error(UserErrorCodes.Unauthorized, "Invalid credentials.");

        return await GenerateTokensAsync(user);
    }

    public async Task<TryResult<LoginResult>> TryRefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshTokenResult = await refreshTokenDao.TryFindByTokenAsync(request.RefreshToken);
        
        if (!refreshTokenResult.IsSuccess)
            return refreshTokenResult.Error;
        
        var refreshToken = refreshTokenResult.Value;
        
        // Check if token is expired
        if (refreshToken.ExpiryDate < DateTime.UtcNow)
            return new Error(UserErrorCodes.RefreshTokenExpired, "Refresh token has expired");
        
        // Check if token is revoked
        if (refreshToken.IsRevoked)
            return new Error(UserErrorCodes.RefreshTokenRevoked, "Refresh token has been revoked");
        
        // Get user from the token
        var userResult = await userDao.TryFindByIdAsync(refreshToken.UserId);
        
        if (!userResult.IsSuccess)
            return userResult.Error;
        
        // Revoke the current refresh token
        refreshToken = refreshToken with { IsRevoked = true };
        await refreshTokenDao.TryUpdateAsync(refreshToken);
        
        // Generate new tokens
        return await GenerateTokensAsync(userResult.Value);
    }

    public async Task<TryResult<bool>> TryRevokeRefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshTokenResult = await refreshTokenDao.TryFindByTokenAsync(request.RefreshToken);
        
        if (!refreshTokenResult.IsSuccess)
            return refreshTokenResult.Error;
        
        var refreshToken = refreshTokenResult.Value;
        
        refreshToken = refreshToken with { IsRevoked = true };
        return await refreshTokenDao.TryUpdateAsync(refreshToken);
    }



    public async Task<TryResult<LoginResult>> TryExchangeGoogleCodeAsync(ExchangeCodeRequest request)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = request.Code,
            ["client_id"] = options.Value.Google.ClientId,
            ["client_secret"] = options.Value.Google.ClientSecret,
            ["redirect_uri"] = request.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenRequest));

        if (!tokenResponse.IsSuccessStatusCode)
            return new Error(UserErrorCodes.OAuthError, "Failed to exchange code with Google");

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (tokenResult?.IdToken == null)
            return new Error(UserErrorCodes.OAuthError, "Invalid response from Google");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenResult.IdToken);

        var email = jwt.Claims.First(c => c.Type == "email").Value;
        var sub = jwt.Claims.First(c => c.Type == "sub").Value;

        return await GetOrCreateOAuthUserAsync(email, sub, AuthProvider.Google);
    }

    private async Task<TryResult<LoginResult>> GetOrCreateOAuthUserAsync(string email, string externalId, AuthProvider provider)
    {
        // Try to find existing user by email
        var existingUserResult = await userDao.TryFindByEmailAsync(email);
        
        if (existingUserResult.IsSuccess)
        {
            var user = existingUserResult.Value;
            
            // Check if user trying to login with different provider
            if (user.AuthProvider != provider)
                return new Error(UserErrorCodes.Unauthorized, $"Please sign in with {user.AuthProvider}");

            // Check if externalId matches
            if (user.ExternalId != externalId)
                return new Error(UserErrorCodes.Unauthorized, "Invalid OAuth credentials");

            return await GenerateTokensAsync(user);
        }

        // Create new user if not found
        if (existingUserResult.Error.Code != UserErrorCodes.UserNotFound)
            return existingUserResult.Error;

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

    private async Task<LoginResult> GenerateTokensAsync(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(options.Value.AccessTokenExpirationMinutes);
        
        var refreshToken = GenerateRefreshToken(user.Id);
        
        await refreshTokenDao.TryAddAsync(refreshToken);
        
        return new LoginResult(accessToken, refreshToken.Token, accessTokenExpiry);
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
            Expires = DateTime.UtcNow.AddMinutes(options.Value.AccessTokenExpirationMinutes),
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
            Id: string.Empty, // MongoDB will generate this
            Token: Convert.ToBase64String(randomBytes),
            UserId: userId,
            ExpiryDate: DateTime.UtcNow.AddDays(options.Value.RefreshTokenExpirationDays),
            IsRevoked: false,
            CreatedAt: DateTime.UtcNow
        );
    }

    private static void CreatePasswordHash(string password, out string hash, out string salt)
    {
        using var hmac = new HMACSHA512();
        salt = Convert.ToBase64String(hmac.Key);
        hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
    {
        using var hmac = new HMACSHA512(Convert.FromBase64String(storedSalt));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(Convert.FromBase64String(storedHash));
    }

    private record GoogleTokenResponse([property: JsonPropertyName("id_token")] string IdToken);
}