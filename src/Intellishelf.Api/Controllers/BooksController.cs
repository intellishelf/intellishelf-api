using Intellishelf.Api.Contracts.Books;
using Intellishelf.Api.ImageProcessing;
using Intellishelf.Api.Mappers.Books;
using Intellishelf.Api.Services;
using Intellishelf.Domain.Ai.Services;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Books.Services;
using Intellishelf.Domain.Files.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[ApiController]
[Authorize]
[Route("books")]
public class BooksController(
    IBookMapper mapper,
    IAiService aiService,
    IBookService bookService,
    IFileStorageService fileStorageService,
    IImageFileValidator imageFileValidator,
    IImageFileProcessor imageFileProcessor) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<Book>>> GetBooks([FromQuery] BookQueryParameters queryParameters)
    {
        var result = await bookService.TryGetPagedBooksAsync(CurrentUserId, queryParameters);
        if (!result.IsSuccess)
            return HandleErrorResponse(result.Error);
        return Ok(result.Value);
    }
    
    [HttpGet("all")]
    public async Task<ActionResult<IReadOnlyCollection<Book>>> GetAllBooks()
    {
        var result = await bookService.TryGetBooksAsync(CurrentUserId);
        return Ok(result.Value);
    }

    [HttpGet("{bookId}")]
    public async Task<ActionResult<Book>> GetBook(string bookId)
    {
        var result = await bookService.TryGetBookAsync(CurrentUserId, bookId);
        return result.IsSuccess
            ? Ok(result.Value)
            : HandleErrorResponse(result.Error);
    }

    [HttpPost]
    public async Task<ActionResult<Book>> AddBook([FromForm] BookRequestContractBase contract)
    {
        string? coverImageUrl = null;
        if (contract.ImageFile != null)
        {
            var validationResult = imageFileValidator.Validate(contract.ImageFile);
            if (!validationResult.IsSuccess)
                return HandleErrorResponse(validationResult.Error);

            await using var processedStream = await imageFileProcessor.ProcessAsync(contract.ImageFile, HttpContext.RequestAborted);

            var uploadResult = await fileStorageService.UploadFileAsync(
                CurrentUserId,
                processedStream,
                contract.ImageFile.FileName);
            if (!uploadResult.IsSuccess)
                return HandleErrorResponse(uploadResult.Error);
            coverImageUrl = uploadResult.Value;
        }

        var addRequest = mapper.MapAdd(CurrentUserId, contract, coverImageUrl);
        var result = await bookService.TryAddBookAsync(addRequest);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBook), new { bookId = result.Value.Id }, result.Value)
            : HandleErrorResponse(result.Error);
    }

    [HttpPost("from-isbn")]
    public async Task<ActionResult<Book>> AddBookFromIsbn([FromBody] AddBookFromIsbnContract contract)
    {
        var result = await bookService.TryAddBookFromIsbnAsync(CurrentUserId, contract.Isbn);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBook), new { bookId = result.Value.Id }, result.Value)
            : HandleErrorResponse(result.Error);
    }

    [HttpPut("{bookId}")]
    public async Task<IActionResult> UpdateBook([FromForm] BookRequestContractBase contract,
        [FromRoute] string bookId)
    {
        string? coverImageUrl = null;
        if (contract.ImageFile != null)
        {
            var validationResult = imageFileValidator.Validate(contract.ImageFile);
            if (!validationResult.IsSuccess)
                return HandleErrorResponse(validationResult.Error);

            await using var processedStream = await imageFileProcessor.ProcessAsync(contract.ImageFile, HttpContext.RequestAborted);

            var uploadResult = await fileStorageService.UploadFileAsync(
                CurrentUserId,
                processedStream,
                contract.ImageFile.FileName);

            if (!uploadResult.IsSuccess)
                return HandleErrorResponse(uploadResult.Error);

            coverImageUrl = uploadResult.Value;
        }

        var updateReq = mapper.MapUpdate(CurrentUserId, bookId, contract, coverImageUrl);
        var result = await bookService.TryUpdateBookAsync(updateReq);

        return result.IsSuccess
            ? NoContent()
            : HandleErrorResponse(result.Error);
    }

    [HttpDelete("{bookId}")]
    public async Task<IActionResult> DeleteBook(string bookId)
    {
        var result = await bookService.TryDeleteBookAsync(CurrentUserId, bookId);
        return result.IsSuccess
            ? NoContent()
            : HandleErrorResponse(result.Error);
    }

    [HttpGet]
    [Route("search")]
    public async Task<ActionResult<PagedResult<Book>>> Search([FromQuery] SearchQueryParameters queryParameters)
    {
        var result = await bookService.SearchAsync(CurrentUserId, queryParameters);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleErrorResponse(result.Error);
    }

    [HttpPost("parse-text")]
    public async Task<ActionResult<ParsedBook>> ParseText([FromBody] ParseFromTextContract contract)
    {
        var mockAi = Request.Headers.Any(h => h.Key == "X-Mock-Ai");
        var result = await aiService.ParseBookFromTextAsync(contract.Text, mockAi);
        return Ok(result.Value);
    }
}