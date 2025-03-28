using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Auth.Models;

namespace Intellishelf.Domain.Auth.Services;

public interface IAuthService
{
    Task<TryResult<User>> FindByNameAndPasswordAsync(string userName, string password);
    Task<TryResult<User>> FindByIdAsync(string id);
    Task<TryResult<string>> SignInAsync(string username, string password);
}