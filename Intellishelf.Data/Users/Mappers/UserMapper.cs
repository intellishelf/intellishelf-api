using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Data.Users.Mappers;

public class UserMapper : IUserMapper
{
    public User Map(UserEntity entity) =>
        new(
            Id: entity.Id, 
            Email: entity.Email, 
            PasswordHash: entity.PasswordHash, 
            PasswordSalt: entity.PasswordSalt,
            AuthProvider: entity.AuthProvider,
            ExternalId: entity.ExternalId);

    public UserEntity MapNewUser(NewUser newUser) =>
        new()
        {
            Email = newUser.Email,
            PasswordHash = newUser.PasswordHash,
            PasswordSalt = newUser.PasswordSalt,
            AuthProvider = newUser.AuthProvider,
            ExternalId = newUser.ExternalId
        };
}
