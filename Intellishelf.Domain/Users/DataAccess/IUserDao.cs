using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Domain.Users.DataAccess;

public interface IUserDao
{
    Task<TryResult<User>> TryFindByIdAsync(string id);
    Task<TryResult<User>> TryFindByEmailAsync(string email);
    Task<TryResult<User>> TryAdd(NewUser user);
}