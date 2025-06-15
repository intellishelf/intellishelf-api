using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Books.Services;
using Intellishelf.Domain.Files.Services;
using Moq;
using Xunit;

namespace Intellishelf.Unit.Tests.Books;

public class BookServiceTests
{
    private readonly Mock<IBookDao> _mockBookDao;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly BookService _bookService;

    public BookServiceTests()
    {
        _mockBookDao = new Mock<IBookDao>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _bookService = new BookService(_mockBookDao.Object, _mockFileStorageService.Object);
    }

    [Fact]
    public async Task TryGetBooksAsync_Success_ReturnsBooks()
    {
        // Arrange
        var userId = "user123";
        var books = new List<Book>
        {
            new Book { Id = "1", Title = "Book 1", UserId = userId, CreatedDate = DateTime.UtcNow }
        };
        _mockBookDao.Setup(x => x.GetBooksAsync(userId))
                   .ReturnsAsync(TryResult.Success<IReadOnlyCollection<Book>>(books));

        // Act
        var result = await _bookService.TryGetBooksAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task TryGetBooksAsync_Error_ReturnsError()
    {
        // Arrange
        var userId = "user123";
        var error = new Error("ERROR", "Test error");
        _mockBookDao.Setup(x => x.GetBooksAsync(userId))
                   .ReturnsAsync((TryResult<IReadOnlyCollection<Book>>)error);

        // Act
        var result = await _bookService.TryGetBooksAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ERROR", result.Error.Code);
    }

    [Fact]
    public async Task TryAddBookAsync_Success_ReturnsBook()
    {
        // Arrange
        var request = new AddBookRequest { Title = "New Book", UserId = "user123" };
        var book = new Book { Id = "1", Title = "New Book", UserId = "user123", CreatedDate = DateTime.UtcNow };
        _mockBookDao.Setup(x => x.AddBookAsync(request))
                   .ReturnsAsync(TryResult.Success(book));

        // Act
        var result = await _bookService.TryAddBookAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Book", result.Value.Title);
    }

    [Fact]
    public async Task TryGetPagedBooksAsync_Success_ReturnsPagedResult()
    {
        // Arrange
        var userId = "user123";
        var queryParams = new BookQueryParameters { PageSize = 10, Page = 1 };
        var books = new List<Book>
        {
            new Book { Id = "1", Title = "Book 1", UserId = userId, CreatedDate = DateTime.UtcNow }
        };
        var pagedResult = new PagedResult<Book>(books, 1, 1, 10);
        
        _mockBookDao.Setup(x => x.GetPagedBooksAsync(userId, queryParams))
                   .ReturnsAsync(TryResult.Success(pagedResult));

        // Act
        var result = await _bookService.TryGetPagedBooksAsync(userId, queryParams);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task TryGetBookAsync_Success_ReturnsBook()
    {
        // Arrange
        var userId = "user123";
        var bookId = "book456";
        var book = new Book { Id = bookId, Title = "Test Book", UserId = userId, CreatedDate = DateTime.UtcNow };
        
        _mockBookDao.Setup(x => x.GetBookAsync(userId, bookId))
                   .ReturnsAsync(TryResult.Success(book));

        // Act
        var result = await _bookService.TryGetBookAsync(userId, bookId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(bookId, result.Value.Id);
        Assert.Equal("Test Book", result.Value.Title);
    }

    [Fact]
    public async Task TryUpdateBookAsync_WithoutCoverImage_UpdatesDirectly()
    {
        // Arrange
        var request = new UpdateBookRequest { Id = "book1", UserId = "user123", Title = "Updated Book" };
        
        _mockBookDao.Setup(x => x.TryUpdateBookAsync(request))
                   .ReturnsAsync(TryResult.Success());

        // Act
        var result = await _bookService.TryUpdateBookAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        _mockBookDao.Verify(x => x.TryUpdateBookAsync(request), Times.Once);
        _mockFileStorageService.Verify(x => x.DeleteFileFromUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TryUpdateBookAsync_WithNewCoverImage_DeletesOldAndUpdates()
    {
        // Arrange
        var request = new UpdateBookRequest 
        { 
            Id = "book1", 
            UserId = "user123", 
            Title = "Updated Book",
            CoverImageUrl = "https://newimage.com/cover.jpg"
        };
        
        var existingBook = new Book 
        { 
            Id = "book1", 
            Title = "Old Book", 
            UserId = "user123", 
            CreatedDate = DateTime.UtcNow,
            CoverImageUrl = "https://oldimage.com/cover.jpg"
        };

        _mockBookDao.Setup(x => x.GetBookAsync(request.UserId, request.Id))
                   .ReturnsAsync(TryResult.Success(existingBook));
        _mockBookDao.Setup(x => x.TryUpdateBookAsync(request))
                   .ReturnsAsync(TryResult.Success());

        // Act
        var result = await _bookService.TryUpdateBookAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        _mockFileStorageService.Verify(x => x.DeleteFileFromUrlAsync("https://oldimage.com/cover.jpg"), Times.Once);
        _mockBookDao.Verify(x => x.TryUpdateBookAsync(request), Times.Once);
    }

    [Fact]
    public async Task TryDeleteBookAsync_WithCoverImage_DeletesImageAndBook()
    {
        // Arrange
        var request = new DeleteBookRequest("user123", "book1");
        var existingBook = new Book 
        { 
            Id = "book1", 
            Title = "Book to Delete", 
            UserId = "user123", 
            CreatedDate = DateTime.UtcNow,
            CoverImageUrl = "https://example.com/cover.jpg"
        };

        _mockBookDao.Setup(x => x.GetBookAsync(request.UserId, request.BookId))
                   .ReturnsAsync(TryResult.Success(existingBook));
        _mockBookDao.Setup(x => x.DeleteBookAsync(request))
                   .ReturnsAsync(TryResult.Success());

        // Act
        var result = await _bookService.TryDeleteBookAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        _mockFileStorageService.Verify(x => x.DeleteFileFromUrlAsync("https://example.com/cover.jpg"), Times.Once);
        _mockBookDao.Verify(x => x.DeleteBookAsync(request), Times.Once);
    }

    [Fact]
    public async Task TryDeleteBookAsync_WithoutCoverImage_DeletesBookOnly()
    {
        // Arrange
        var request = new DeleteBookRequest("user123", "book1");
        var existingBook = new Book 
        { 
            Id = "book1", 
            Title = "Book to Delete", 
            UserId = "user123", 
            CreatedDate = DateTime.UtcNow,
            CoverImageUrl = null
        };

        _mockBookDao.Setup(x => x.GetBookAsync(request.UserId, request.BookId))
                   .ReturnsAsync(TryResult.Success(existingBook));
        _mockBookDao.Setup(x => x.DeleteBookAsync(request))
                   .ReturnsAsync(TryResult.Success());

        // Act
        var result = await _bookService.TryDeleteBookAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        _mockFileStorageService.Verify(x => x.DeleteFileFromUrlAsync(It.IsAny<string>()), Times.Never);
        _mockBookDao.Verify(x => x.DeleteBookAsync(request), Times.Once);
    }
}
