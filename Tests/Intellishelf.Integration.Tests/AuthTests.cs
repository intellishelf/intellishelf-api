using System.Net;
using System.Net.Http.Json;
using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Data.Users.Entities;
using Intellishelf.Domain.Users.Helpers;
using Intellishelf.Domain.Users.Models;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class AuthTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbFixture _mongoDbFixture;

    private const string TestUserEmail = "user@test.com";
    private const string TestUserPassword = "SecurePassword123!";

    public AuthTests(MongoDbFixture mongoDbFixture, AzuriteFixture azuriteFixture)
    {
        _factory = new TestWebApplicationFactory(mongoDbFixture, azuriteFixture);
        _client = _factory.CreateClient();
        _mongoDbFixture = mongoDbFixture;
    }

    [Fact]
    private async Task GivenValidUserData_WhenRegisteringUser_ThenUserIsCreated()
    {
        // Arrange
        var registerRequest = new RegisterUserRequestContract("newuser@test.com", TestUserPassword);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<LoginResultContract>();
        Assert.NotNull(responseContent?.AccessToken);
    }

    [Fact]
    private async Task GivenInvalidUserData_WhenRegisteringUser_ThenErrorIsReturned()
    {
        // Arrange
        var registerRequest = new RegisterUserRequestContract(TestUserEmail, "123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(responseContent);
    }

    [Fact]
    private async Task GivenNonExistingUser_WhenTryToLogin_ThenUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequestContract("nonexistinguser@test.com", TestUserPassword);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    private async Task GivenExistingUser_WhenTryToLogin_ThenAuthorized()
    {
        // Arrange
        var (hash, salt) = AuthHelper.CreatePasswordHash(TestUserPassword);

        await _mongoDbFixture
            .Database
            .GetCollection<UserEntity>(UserEntity.CollectionName)
            .InsertOneAsync(new UserEntity
            {
                AuthProvider = AuthProvider.Email,
                Email = TestUserEmail,
                PasswordHash = hash,
                PasswordSalt = salt
            });

        var loginRequest = new LoginRequestContract(TestUserEmail, TestUserPassword);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<LoginResultContract>();
        Assert.NotNull(responseContent?.AccessToken);
    }

    public void Dispose() => _factory.Dispose();
}