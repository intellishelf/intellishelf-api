using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Data.Users.Mappers;

public class UserMapper : IUserMapper
{
    public User Map(UserEntity entity) =>
        new(entity.Id, entity.Email, entity.PasswordHash, entity.PasswordSalt);

    public UserEntity MapNewUser(NewUser model) =>
        new()
        {
            Email = model.Email,
            PasswordHash = model.PasswordHash,
            PasswordSalt = model.PasswordSalt
        };
}