using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Data.Users.Mappers;

public class RefreshTokenMapper : IRefreshTokenMapper
{
    public RefreshToken Map(RefreshTokenEntity entity) =>
        new(
            entity.Id,
            entity.Token,
            entity.UserId,
            entity.ExpiryDate,
            entity.IsRevoked,
            entity.CreatedAt);

    public RefreshTokenEntity Map(RefreshToken model) =>
        new()
        {
            Id = model.Id,
            Token = model.Token,
            UserId = model.UserId,
            ExpiryDate = model.ExpiryDate,
            IsRevoked = model.IsRevoked,
            CreatedAt = model.CreatedAt
        };
}
