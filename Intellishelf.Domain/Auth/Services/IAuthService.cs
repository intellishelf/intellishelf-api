using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Auth.Models;

namespace Intellishelf.Domain.Auth.Services;

public interface IAuthService
{
    Task<TryResult<User>> TryFindByNameAndPasswordAsync(string userName, string password);
    Task<TryResult<User>> TryFindByIdAsync(string id);
    Task<TryResult<string>> TrySignInAsync(LoginRequest request);
}