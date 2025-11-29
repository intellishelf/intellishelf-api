using Intellishelf.Common.TryResult;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Data.Users.Mappers;
using Intellishelf.Domain.Users.DataAccess;
using Intellishelf.Domain.Users.Models;
using MongoDB.Driver;

namespace Intellishelf.Data.Users.DataAccess;

public class RefreshTokenDao(IMongoDatabase database, IRefreshTokenMapper mapper) : IRefreshTokenDao
{
    private readonly IMongoCollection<RefreshTokenEntity> _refreshTokensCollection = database.GetCollection<RefreshTokenEntity>("RefreshTokens");

    public async Task<TryResult<RefreshToken>> TryAddAsync(RefreshToken refreshToken)
    {
        var entity = mapper.Map(refreshToken);
        
        await _refreshTokensCollection.InsertOneAsync(entity);
        
        return mapper.Map(entity);
    }

    public async Task<TryResult<RefreshToken>> TryFindByTokenAsync(string token)
    {
        var refreshToken = await _refreshTokensCollection.Find(rt => rt.Token == token).FirstOrDefaultAsync();
        
        if (refreshToken == null)
            return new Error(UserErrorCodes.RefreshTokenNotFound, "Refresh token not found");
        
        return mapper.Map(refreshToken);
    }

    public async Task<TryResult<bool>> TryUpdateAsync(RefreshToken refreshToken)
    {
        var entity = mapper.Map(refreshToken);
        
        var result = await _refreshTokensCollection.ReplaceOneAsync(
            rt => rt.Id == refreshToken.Id,
            entity);
        
        if (result.ModifiedCount == 0)
            return new Error(UserErrorCodes.RefreshTokenNotFound, "Refresh token not found");
        
        return true;
    }

    public async Task<TryResult<bool>> TryDeleteExpiredTokensAsync()
    {
        await _refreshTokensCollection.DeleteManyAsync(
            rt => rt.ExpiryDate < DateTime.UtcNow || rt.IsRevoked);
        
        return true;
    }

    public async Task<TryResult<IEnumerable<RefreshToken>>> TryFindByUserIdAsync(string userId)
    {
        var refreshTokens = await _refreshTokensCollection.Find(rt => rt.UserId == userId).ToListAsync();

        return refreshTokens.Select(mapper.Map).ToList();
    }

    public async Task<TryResult<long>> TryDeleteAllByUserIdAsync(string userId)
    {
        var result = await _refreshTokensCollection.DeleteManyAsync(rt => rt.UserId == userId);
        return result.DeletedCount;
    }
}