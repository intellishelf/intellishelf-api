using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Domain.Users.DataAccess;

public interface IUserDao
{
    Task<TryResult<User>> TryFindByIdAsync(string id);
    Task<TryResult<User>> TryFindByEmailAsync(string email);
    Task<TryResult<bool>> TryUserExists(string email);
    Task<TryResult<User>> TryAdd(NewUser user);
    Task<TryResult<bool>> TryDeleteUserAsync(string userId);
}
