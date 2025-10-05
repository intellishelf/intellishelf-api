using System.Net;
using System.Net.Http.Headers;
using Intellishelf.Integration.Tests.Infra;
using Intellishelf.Integration.Tests.Infra.Fixtures;
using Xunit;

namespace Intellishelf.Integration.Tests;

[Collection("Integration Tests")]
public sealed class BooksTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BooksTests(MongoDbFixture mongoDbFixture, AzuriteFixture azuriteFixture)
    {
        _factory = new TestWebApplicationFactory(mongoDbFixture, azuriteFixture);
        _client = _factory.CreateClient();
    }

    [Fact]
    private async Task GivenTestAuthScheme_WhenAccessProtectedEndpoint_ThenSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/books/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    private async Task GivenValidBookData_WhenPostNewBookWithFile_ThenCreated()
    {
        // Arrange
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent("Test Book Title"), "Title");
        content.Add(new StringContent("Test Author"), "Author");
        content.Add(new StringContent("Test description"), "Description");
        content.Add(new StringContent("2024"), "PublishedYear");

        var imageContent = new ByteArrayContent("fake image data"u8.ToArray());
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent, "ImageFile", "test-cover.jpg");

        // Act
        var response = await _client.PostAsync("/api/books", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Book Title", responseContent);
    }


    public void Dispose()
    {
        _factory.Dispose();
    }
}