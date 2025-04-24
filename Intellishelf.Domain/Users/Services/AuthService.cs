using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.ErrorCodes;
using Intellishelf.Domain.Users.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Intellishelf.Domain.Users.Services;

public class AuthService(IOptions<AuthConfig> options, IUserDao userDao) : IAuthService
{
    public async Task<TryResult<User>> TryFindByIdAsync(string id) =>
        await userDao.TryFindByIdAsync(id);

    public async Task<TryResult<LoginResult>> TryRegisterAsync(RegisterUserRequest request)
    {
        var existingUserResult = await userDao.TryFindByEmailAsync(request.Email);

        if (existingUserResult.IsSuccess || existingUserResult.Error.Code != UserErrorCodes.UserNotFound)
            return new Error(UserErrorCodes.AlreadyExists, $"User with email {request.Email} already exists.");

        CreatePasswordHash(request.Password, out var passwordHash, out var passwordSalt);

        var result = await userDao.TryAdd(new NewUser(request.Email, passwordHash, passwordSalt));

        if(!result.IsSuccess)
            return result.Error;

        var token = GenerateJwtToken(result.Value);
        return new LoginResult(token);
    }

    public async Task<TryResult<LoginResult>> TrySignInAsync(LoginRequest request)
    {
        var result = await userDao.TryFindByEmailAsync(request.Email);

        if (!result.IsSuccess)
            return result.Error;

        var verifyHashResult =
            VerifyPasswordHash(request.Password, result.Value.PasswordHash, result.Value.PasswordSalt);

        var token = GenerateJwtToken(result.Value);
        return verifyHashResult
            ? new LoginResult(token)
            : new Error(UserErrorCodes.Unauthorized, "Invalid credentials.");
    }

    private string GenerateJwtToken(User user)
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
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = credentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
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
}