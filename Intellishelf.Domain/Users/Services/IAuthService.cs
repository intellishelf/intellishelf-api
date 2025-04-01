using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Domain.Users.Services;

public interface IAuthService
{
    Task<TryResult<User>> TryFindByIdAsync(string id);
    Task<TryResult<LoginResult>> TrySignInAsync(LoginRequest request);
    Task<TryResult<LoginResult>> TryRegisterAsync(RegisterUserRequest request);
}