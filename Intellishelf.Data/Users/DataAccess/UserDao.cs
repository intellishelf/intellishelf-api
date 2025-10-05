using Intellishelf.Common.TryResult;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Data.Users.Mappers;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.ErrorCodes;
using Intellishelf.Domain.Users.Models;
using MongoDB.Driver;

namespace Intellishelf.Data.Users.DataAccess;

public class UserDao(IMongoDatabase database, IUserMapper mapper) : IUserDao
{
    private readonly IMongoCollection<UserEntity> _usersCollection = database.GetCollection<UserEntity>(UserEntity.CollectionName);

    public async Task<TryResult<User>> TryFindByIdAsync(string id)
    {
        var user = await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();

        if (user == null)
            return new Error(UserErrorCodes.UserNotFound, $"User with id {id} not found");

        return mapper.Map(user);
    }

    public async Task<TryResult<User>> TryFindByEmailAsync(string email)
    {
        var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

        if (user == null)
            return new Error(UserErrorCodes.UserNotFound, $"User with email {email} not found");

        return mapper.Map(user);
    }

    public async Task<TryResult<bool>> TryUserExists(string email)
    {
        var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

        return user != null;
    }

    public async Task<TryResult<User>> TryAdd(NewUser user)
    {
        var entity = mapper.MapNewUser(user);

        await _usersCollection.InsertOneAsync(entity);

        return mapper.Map(entity);
    }
}