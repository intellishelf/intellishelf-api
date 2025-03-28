using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Auth.Models;

namespace Intellishelf.Domain.Auth.DataAccess;

public interface IUserDao
{
    Task<TryResult<User>> FindByNameAndPasswordAsync(string userName, string password);
    Task<TryResult<User>> FindByIdAsync(string id);
}