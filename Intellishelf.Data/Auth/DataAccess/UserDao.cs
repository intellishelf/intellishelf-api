using Intellishelf.Common.TryResult;
using Intellishelf.Data.Auth.Entities;
using Intellishelf.Domain.Auth.DataAccess;
using Intellishelf.Domain.Auth.ErrorCodes;
using Intellishelf.Domain.Auth.Models;
using MongoDB.Driver;

namespace Intellishelf.Data.Auth.DataAccess;

public class UserDao(IMongoDatabase database) : IUserDao
{
    private readonly IMongoCollection<UserEntity> _usersCollection = database.GetCollection<UserEntity>("Users");

    public async Task<TryResult<User>> FindByNameAndPasswordAsync(string userName, string password)
    {
        var user = await _usersCollection.Find(u => u.UserName == userName && u.Password == password).FirstOrDefaultAsync();

        if (user == null)
            return new Error(AuthErrorCodes.UserNotFound, $"User {userName} not found");

        return new User(user.Id, user.UserName);
    }

    public async Task<TryResult<User>> FindByIdAsync(string id)
    {
        var user = await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();

        if (user == null)
            return new Error(AuthErrorCodes.UserNotFound, $"User {id} not found");

        return new User(user.Id, user.UserName);
    }
}