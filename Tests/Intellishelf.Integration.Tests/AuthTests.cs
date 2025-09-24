using System.Net;
using System.Net.Http.Json;
using Intellishelf.Api.Contracts.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Intellishelf.Integration.Tests;

public class AuthTests
{
    private static readonly HttpClient Client = new WebApplicationFactory<Program>().CreateClient();

    [Fact]
    private async Task GivenNonExistingUser_WhenTryToLogin_ThenUnauthorized()
    {
        //Arrange
        var loginRequest = new LoginRequestContract("foo@bar", "bar");

        //Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        //Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}