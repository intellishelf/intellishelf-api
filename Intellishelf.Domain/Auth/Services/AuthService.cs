using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Auth.DataAccess;
using Intellishelf.Domain.Auth.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Intellishelf.Domain.Auth.Config;

namespace Intellishelf.Domain.Auth.Services;

public class AuthService(IUserDao userDao, IOptions<AuthConfig> options) : IAuthService
{
    public async Task<TryResult<User>> FindByNameAndPasswordAsync(string userName, string password)
    {
        return await userDao.FindByNameAndPasswordAsync(userName, password);
    }

    public async Task<TryResult<User>> FindByIdAsync(string id)
    {
        return await userDao.FindByIdAsync(id);
    }

    public async Task<TryResult<string>> SignInAsync(string username, string password)
    {
        var result = await FindByNameAndPasswordAsync(username, password);

        if (!result.IsSuccess)
            return result.Error;

        var user = result.Value;

        var tokenHandler = new JwtSecurityTokenHandler();
        var keyBytes = Encoding.ASCII.GetBytes(options.Value.Key);

        var claims = new[]
        {
            new Claim("userId", user.UserId),
            new Claim("userName", user.UserName)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(30),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}