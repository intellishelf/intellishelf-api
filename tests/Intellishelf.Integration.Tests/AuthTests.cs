using System.Net;
using System.Net.Http.Json;
using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class AuthTests : IAsyncLifetime, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MongoDbFixture _mongoDbFixture;
    private const string WeakPassword = "123";

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
        var registerRequest = new RegisterUserRequestContract("newuser@test.com", DefaultTestUsers.Authenticated.Password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<LoginResultContract>(TestContext.Current.CancellationToken);
        Assert.NotNull(responseContent?.AccessToken);
    }

    [Fact]
    private async Task GivenInvalidUserData_WhenRegisteringUser_ThenErrorIsReturned()
    {
        // Arrange
        var registerRequest = new RegisterUserRequestContract(DefaultTestUsers.Authenticated.Email, WeakPassword);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(responseContent);
    }

    [Fact]
    private async Task GivenNonExistingUser_WhenTryToLogin_ThenUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequestContract("nonexistinguser@test.com", DefaultTestUsers.Authenticated.Password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    private async Task GivenExistingUser_WhenTryToLogin_ThenAuthorized()
    {
        // Arrange
        var loginRequest = new LoginRequestContract(DefaultTestUsers.Authenticated.Email, DefaultTestUsers.Authenticated.Password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadFromJsonAsync<LoginResultContract>(TestContext.Current.CancellationToken);
        Assert.NotNull(responseContent?.AccessToken);
    }

    [Fact]
    private async Task GivenAuthenticatedUser_WhenRequestingProfile_ThenUserDetailsReturned()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadFromJsonAsync<UserResponseContract>(TestContext.Current.CancellationToken);
        Assert.NotNull(responseContent);
        Assert.Equal(DefaultTestUsers.Authenticated.Id, responseContent.Id);
        Assert.Equal(DefaultTestUsers.Authenticated.Email, responseContent.Email);
    }

    public void Dispose() => _factory.Dispose();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public ValueTask InitializeAsync() => new ValueTask(_mongoDbFixture.SeedDefaultUserAsync());
}