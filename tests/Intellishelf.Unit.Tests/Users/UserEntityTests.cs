using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Users.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Intellishelf.Unit.Tests.Users;

public class UserEntityTests
{
    [Fact]
    public void GivenOldUserDocument_WhenDeserialized_ThenProDefaultsToFalse()
    {
        var document = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Email", "synthetic@example.test" },
            { "AuthProvider", (int)AuthProvider.Email },
            { "FutureField", true }
        };

        var user = BsonSerializer.Deserialize<UserEntity>(document);

        Assert.False(user.IsPro);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GivenUser_WhenSerialized_ThenProIsOnlyWrittenWhenTrue(bool isPro)
    {
        var user = new UserEntity { Email = "synthetic@example.test", AuthProvider = AuthProvider.Email, IsPro = isPro };

        var document = user.ToBsonDocument();

        Assert.Equal(isPro, document.Contains("IsPro"));
        if (isPro)
            Assert.True(document["IsPro"].AsBoolean);
    }
}
