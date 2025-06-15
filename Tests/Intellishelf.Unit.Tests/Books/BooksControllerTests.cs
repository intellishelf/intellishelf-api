using System.Security.Claims;
using Intellishelf.Api.Contracts.Books;
using Intellishelf.Api.Controllers;
using Intellishelf.Api.Mappers.Books;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Ai.Services;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Books.Services;
using Intellishelf.Domain.Files.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Intellishelf.Unit.Tests.Books;

public class BooksControllerTests
{
    private readonly Mock<IBookMapper> _mockBookMapper;
    private readonly Mock<IAiService> _mockAiService;
    private readonly Mock<IBookService> _mockBookService;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly BooksController _controller;
    private const string TestUserId = "user123";

    public BooksControllerTests()
    {
        _mockBookMapper = new Mock<IBookMapper>();
        _mockAiService = new Mock<IAiService>();
        _mockBookService = new Mock<IBookService>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        
        _controller = new BooksController(
            _mockBookMapper.Object,
            _mockAiService.Object,
            _mockBookService.Object,
            _mockFileStorageService.Object);

        // Mock the CurrentUserId property
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, TestUserId)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetBooks_Success_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParams = new BookQueryParameters { PageSize = 10, Page = 1 };
        var books = new List<Book>
        {
            new Book { Id = "1", Title = "Book 1", UserId = TestUserId, CreatedDate = DateTime.UtcNow }
        };
        var pagedResult = new PagedResult<Book>(books, 1, 1, 10);
        
        _mockBookService.Setup(x => x.TryGetPagedBooksAsync(TestUserId, queryParams))
                       .ReturnsAsync(TryResult.Success(pagedResult));

        // Act
        var result = await _controller.GetBooks(queryParams);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<PagedResult<Book>>(okResult.Value);
        Assert.Single(returnValue.Items);
        Assert.Equal(1, returnValue.TotalCount);
    }

    [Fact]
    public async Task GetBooks_ServiceError_ReturnsErrorResponse()
    {
        // Arrange
        var queryParams = new BookQueryParameters { PageSize = 10, Page = 1 };
        var error = new Error(BookErrorCodes.BookNotFound, "Books not found");
        
        _mockBookService.Setup(x => x.TryGetPagedBooksAsync(TestUserId, queryParams))
                       .ReturnsAsync((TryResult<PagedResult<Book>>)error);

        // Act
        var result = await _controller.GetBooks(queryParams);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetAllBooks_Success_ReturnsOkWithBooks()
    {
        // Arrange
        var books = new List<Book>
        {
            new Book { Id = "1", Title = "Book 1", UserId = TestUserId, CreatedDate = DateTime.UtcNow },
            new Book { Id = "2", Title = "Book 2", UserId = TestUserId, CreatedDate = DateTime.UtcNow }
        };
        
        _mockBookService.Setup(x => x.TryGetBooksAsync(TestUserId))
                       .ReturnsAsync(TryResult.Success<IReadOnlyCollection<Book>>(books));

        // Act
        var result = await _controller.GetAllBooks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<List<Book>>(okResult.Value);
        Assert.Equal(2, returnValue.Count);
    }

    [Fact]
    public async Task GetBook_Success_ReturnsOkWithBook()
    {
        // Arrange
        var bookId = "book123";
        var book = new Book { Id = bookId, Title = "Test Book", UserId = TestUserId, CreatedDate = DateTime.UtcNow };
        
        _mockBookService.Setup(x => x.TryGetBookAsync(TestUserId, bookId))
                       .ReturnsAsync(TryResult.Success(book));

        // Act
        var result = await _controller.GetBook(bookId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Book>(okResult.Value);
        Assert.Equal(bookId, returnValue.Id);
        Assert.Equal("Test Book", returnValue.Title);
    }

    [Fact]
    public async Task GetBook_BookNotFound_ReturnsNotFound()
    {
        // Arrange
        var bookId = "nonexistent";
        var error = new Error(BookErrorCodes.BookNotFound, "Book not found");
        
        _mockBookService.Setup(x => x.TryGetBookAsync(TestUserId, bookId))
                       .ReturnsAsync((TryResult<Book>)error);

        // Act
        var result = await _controller.GetBook(bookId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public async Task AddBook_WithoutImageFile_Success_ReturnsCreatedAtAction()
    {
        // Arrange
        var contract = new BookRequestContractBase 
        { 
            Title = "New Book",
            Authors = new[] { "Author 1" },
            ImageFile = null
        };
        var addRequest = new AddBookRequest { Title = "New Book", UserId = TestUserId };
        var createdBook = new Book { Id = "book123", Title = "New Book", UserId = TestUserId, CreatedDate = DateTime.UtcNow };
        
        _mockBookMapper.Setup(x => x.MapAdd(TestUserId, contract, null))
                      .Returns(addRequest);
        _mockBookService.Setup(x => x.TryAddBookAsync(addRequest))
                       .ReturnsAsync(TryResult.Success(createdBook));

        // Act
        var result = await _controller.AddBook(contract);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(BooksController.GetBook), createdResult.ActionName);
        Assert.Equal("book123", createdResult.RouteValues!["bookId"]);
        var returnValue = Assert.IsType<Book>(createdResult.Value);
        Assert.Equal("New Book", returnValue.Title);
    }

    [Fact]
    public async Task AddBook_WithImageFile_Success_ReturnsCreatedAtAction()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        
        var contract = new BookRequestContractBase 
        { 
            Title = "New Book",
            ImageFile = mockFile.Object
        };
        var coverImageUrl = "https://example.com/cover.jpg";
        var addRequest = new AddBookRequest { Title = "New Book", UserId = TestUserId, CoverImageUrl = coverImageUrl };
        var createdBook = new Book { Id = "book123", Title = "New Book", UserId = TestUserId, CreatedDate = DateTime.UtcNow };
        
        _mockFileStorageService.Setup(x => x.UploadFileAsync(TestUserId, It.IsAny<Stream>(), "test.jpg"))
                              .ReturnsAsync(TryResult.Success(coverImageUrl));
        _mockBookMapper.Setup(x => x.MapAdd(TestUserId, contract, coverImageUrl))
                      .Returns(addRequest);
        _mockBookService.Setup(x => x.TryAddBookAsync(addRequest))
                       .ReturnsAsync(TryResult.Success(createdBook));

        // Act
        var result = await _controller.AddBook(contract);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("book123", createdResult.RouteValues!["bookId"]);
    }

    [Fact]
    public async Task AddBook_FileUploadFails_ReturnsErrorResponse()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        
        var contract = new BookRequestContractBase 
        { 
            Title = "New Book",
            ImageFile = mockFile.Object
        };
        var uploadError = new Error("UPLOAD_FAILED", "File upload failed");
        
        _mockFileStorageService.Setup(x => x.UploadFileAsync(TestUserId, It.IsAny<Stream>(), "test.jpg"))
                              .ReturnsAsync((TryResult<string>)uploadError);

        // Act
        var result = await _controller.AddBook(contract);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateBook_WithoutImageFile_Success_ReturnsNoContent()
    {
        // Arrange
        var bookId = "book123";
        var contract = new BookRequestContractBase 
        { 
            Title = "Updated Book",
            ImageFile = null
        };
        var updateRequest = new UpdateBookRequest { Id = bookId, Title = "Updated Book", UserId = TestUserId };
        
        _mockBookMapper.Setup(x => x.MapUpdate(TestUserId, bookId, contract, null))
                      .Returns(updateRequest);
        _mockBookService.Setup(x => x.TryUpdateBookAsync(updateRequest))
                       .ReturnsAsync(TryResult.Success());

        // Act
        var result = await _controller.UpdateBook(contract, bookId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateBook_WithImageFile_Success_ReturnsNoContent()
    {
        // Arrange
        var bookId = "book123";
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("updated.jpg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        
        var contract = new BookRequestContractBase 
        { 
            Title = "Updated Book",
            ImageFile = mockFile.Object
        };
        var coverImageUrl = "https://example.com/updated-cover.jpg";
        var updateRequest = new UpdateBookRequest { Id = bookId, Title = "Updated Book", UserId = TestUserId, CoverImageUrl = coverImageUrl };
        
        _mockFileStorageService.Setup(x => x.UploadFileAsync(TestUserId, It.IsAny<Stream>(), "updated.jpg"))
                              .ReturnsAsync(TryResult.Success(coverImageUrl));
        _mockBookMapper.Setup(x => x.MapUpdate(TestUserId, bookId, contract, coverImageUrl))
                      .Returns(updateRequest);
        _mockBookService.Setup(x => x.TryUpdateBookAsync(updateRequest))
                       .ReturnsAsync(TryResult.Success());

        // Act
        var result = await _controller.UpdateBook(contract, bookId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateBook_ServiceError_ReturnsErrorResponse()
    {
        // Arrange
        var bookId = "book123";
        var contract = new BookRequestContractBase 
        { 
            Title = "Updated Book",
            ImageFile = null
        };
        var updateRequest = new UpdateBookRequest { Id = bookId, Title = "Updated Book", UserId = TestUserId };
        var error = new Error(BookErrorCodes.BookNotFound, "Book not found");
        
        _mockBookMapper.Setup(x => x.MapUpdate(TestUserId, bookId, contract, null))
                      .Returns(updateRequest);
        _mockBookService.Setup(x => x.TryUpdateBookAsync(updateRequest))
                       .ReturnsAsync((TryResult)error);

        // Act
        var result = await _controller.UpdateBook(contract, bookId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_Success_ReturnsNoContent()
    {
        // Arrange
        var bookId = "book123";
        var deleteRequest = new DeleteBookRequest(TestUserId, bookId);
        
        _mockBookMapper.Setup(x => x.MapDelete(TestUserId, bookId))
                      .Returns(deleteRequest);
        _mockBookService.Setup(x => x.TryDeleteBookAsync(deleteRequest))
                       .ReturnsAsync(TryResult.Success());

        // Act
        var result = await _controller.DeleteBook(bookId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteBook_ServiceError_ReturnsErrorResponse()
    {
        // Arrange
        var bookId = "book123";
        var deleteRequest = new DeleteBookRequest(TestUserId, bookId);
        var error = new Error(BookErrorCodes.BookNotFound, "Book not found");
        
        _mockBookMapper.Setup(x => x.MapDelete(TestUserId, bookId))
                      .Returns(deleteRequest);
        _mockBookService.Setup(x => x.TryDeleteBookAsync(deleteRequest))
                       .ReturnsAsync((TryResult)error);

        // Act
        var result = await _controller.DeleteBook(bookId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }
} 