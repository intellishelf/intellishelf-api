using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Domain.Users.DataAccess;

public interface IRefreshTokenDao
{
    Task<TryResult<RefreshToken>> TryAddAsync(RefreshToken refreshToken);
    Task<TryResult<RefreshToken>> TryFindByTokenAsync(string token);
    Task<TryResult<bool>> TryUpdateAsync(RefreshToken refreshToken);
    Task<TryResult<bool>> TryDeleteExpiredTokensAsync();
    Task<TryResult<IEnumerable<RefreshToken>>> TryFindByUserIdAsync(string userId);
}
