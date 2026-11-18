using Intellishelf.Data.Users.Entities;
using Intellishelf.Data.Users.Mappers;
using Intellishelf.Domain.Users.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Intellishelf.Unit.Tests.Users;

public class UserMapperTests
{
    private readonly UserMapper _mapper = new();

    private static UserEntity CreateEntity(string? displayName) => new()
    {
        Email = "reader@example.com",
        AuthProvider = AuthProvider.Email,
        DisplayName = displayName
    };

    [Fact]
    public void Map_WithDisplayName_CopiesValue() =>
        Assert.Equal("Reader", _mapper.Map(CreateEntity("Reader")).DisplayName);

    [Fact]
    public void Map_WithoutDisplayName_ReturnsNull() =>
        Assert.Null(_mapper.Map(CreateEntity(null)).DisplayName);

    [Fact]
    public void Deserialize_LegacyDocumentWithoutDisplayName_LoadsWithNull()
    {
        var document = CreateEntity(null).ToBsonDocument();
        document.Remove("DisplayName");

        var entity = BsonSerializer.Deserialize<UserEntity>(document);

        Assert.Null(_mapper.Map(entity).DisplayName);
    }
}
