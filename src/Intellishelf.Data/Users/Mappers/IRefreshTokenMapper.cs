using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Data.Users.Mappers;

public interface IRefreshTokenMapper
{
    RefreshToken Map(RefreshTokenEntity entity);
    RefreshTokenEntity Map(RefreshToken model);
}
