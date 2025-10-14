using Intellishelf.Domain.Users.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Intellishelf.Data.Users.Entities;

public class UserEntity : EntityBase
{
    public static string CollectionName => "Users";

    public required string Email { get; init; }

    public string? PasswordHash { get; init; }

    public string? PasswordSalt { get; init; }

    public required AuthProvider AuthProvider { get; init; }

    public string? ExternalId { get; init; }
}