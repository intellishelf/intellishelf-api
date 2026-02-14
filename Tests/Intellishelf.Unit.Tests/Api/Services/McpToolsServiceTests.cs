using Intellishelf.Api.Mcp.Tools;
using Intellishelf.Api.Services;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Models;
using Moq;

namespace Intellishelf.Unit.Tests.Api.Services;

public class McpToolsServiceTests
{
    [Fact]
    public async Task ExecuteToolAsync_ShouldThrowInvalidOperationException_WhenGetBooksByAuthorArgumentsAreMalformedJson()
    {
        var service = CreateService();

        var act = () => service.ExecuteToolAsync("user-1", "get_books_by_author", "{bad-json");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal("Invalid arguments for get_books_by_author", exception.Message);
    }

    [Fact]
    public async Task ExecuteToolAsync_ShouldThrowInvalidOperationException_WhenGetBooksByAuthorArgumentsMissingAuthor()
    {
        var service = CreateService();

        var act = () => service.ExecuteToolAsync("user-1", "get_books_by_author", "{}");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal("Missing required 'author' argument for get_books_by_author", exception.Message);
    }

    private static McpToolsService CreateService()
    {
        var bookDaoMock = new Mock<IBookDao>();
        bookDaoMock
            .Setup(dao => dao.GetBooksAsync(It.IsAny<string>()))
            .ReturnsAsync(TryResult.Success<IReadOnlyCollection<Book>>(new List<Book>()));

        var getAllBooksTool = new GetAllBooksTool(bookDaoMock.Object);
        var getBooksByAuthorTool = new GetBooksByAuthorTool(bookDaoMock.Object);

        return new McpToolsService(getAllBooksTool, getBooksByAuthorTool);
    }
}
