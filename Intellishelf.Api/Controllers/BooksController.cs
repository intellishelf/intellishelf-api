using Intellishelf.Api.Contracts.Books;
using Intellishelf.Api.Mappers.Books;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Ai.Services;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Books.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[ApiController]
[Authorize]
[Route("books")]
public class BooksController(
    IBookMapper mapper,
    IAiService aiService,
    IBookService bookService) : ApiControllerBase
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

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<Book>> AddBook([FromForm] BookRequestContractBase contract)
    {
        var result = await bookService.TryAddBookAsync(mapper.MapAdd(CurrentUserId, contract));

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBook), new { bookId = result.Value.Id }, result.Value)
            : HandleErrorResponse(result.Error);
    }


    [HttpPut("{bookId}")]
    public async Task<ActionResult<string>> UpdateBook([FromForm] BookRequestContractBase contract,
        [FromRoute] string bookId)
    {
        var updateResult = await bookService.TryUpdateBookAsync(mapper.MapUpdate(CurrentUserId, bookId, contract));

        return updateResult.IsSuccess
            ? NoContent()
            : HandleErrorResponse(updateResult.Error);
    }

    [HttpDelete("{bookId}")]
    public async Task<IActionResult> DeleteBook(string bookId)
    {
        await bookService.TryDeleteBookAsync(mapper.MapDelete(CurrentUserId, bookId));

        return NoContent();
    }

    [HttpPost("parse-text")]
    public async Task<ActionResult<ParsedBook>> ParseText([FromBody] ParseFromTextContract contract)
    {
        var mockAi = Request.Headers.Any(h => h.Key == "X-Mock-Ai");

        var result = await aiService.ParseBookFromTextAsync(contract.Text, mockAi);

        return Ok(result.Value);
    }

    // [HttpPost("parse-image")]
    // public async Task<ActionResult<ParsedBookResponseContract>> ParseImage([FromForm] IFormFile file)
    // {
    //     if (file == null || file.Length == 0)
    //         return BadRequest("File is empty");
    //
    //     using (var stream = file.OpenReadStream())
    //     {
    //         var result = await aiService.ParseBookAsync(stream);
    //         return Ok(result);
    //     }
    // }
}
