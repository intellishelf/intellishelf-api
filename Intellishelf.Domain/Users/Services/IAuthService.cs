using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Domain.Users.Services;

public interface IAuthService
{
    Task<TryResult<LoginResult>> TrySignInAsync(LoginRequest request);
    Task<TryResult<LoginResult>> TryRegisterAsync(RegisterUserRequest request);
    Task<TryResult<LoginResult>> TrySignInExternalAsync(ExternalLoginRequest request);
    Task<TryResult<LoginResult>> TryRefreshTokenAsync(RefreshTokenRequest request);
    Task<TryResult<bool>> TryRevokeRefreshTokenAsync(RefreshTokenRequest request);
    Task<TryResult<User>> TryFindByIdAsync(string id);
}