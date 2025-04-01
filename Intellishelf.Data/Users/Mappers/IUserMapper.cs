using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Data.Users.Mappers;

public interface IUserMapper
{
    User Map(UserEntity entity);
    UserEntity MapNewUser(NewUser model);
}