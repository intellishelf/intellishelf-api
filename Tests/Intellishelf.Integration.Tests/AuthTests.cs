using System.Net;
using System.Net.Http.Json;
using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Integration.Tests.Infra;
using Xunit;

namespace Intellishelf.Integration.Tests;

public sealed class AuthTests: IClassFixture<MongoDbFixture>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthTests(MongoDbFixture mongoDbFixture)
    {
        _factory = new TestWebApplicationFactory(mongoDbFixture);
        _client = _factory.CreateClient();
    }

    [Fact]
    private async Task GivenNonExistingUser_WhenTryToLogin_ThenUnauthorized()
    {
        //Arrange
        var loginRequest = new LoginRequestContract("foo@bar", "123");

        //Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        //Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    private async Task GivenTestAuthScheme_WhenAccessProtectedEndpoint_ThenSuccess()
    {
        // Act
        var response = await _client.GetAsync("/books/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}