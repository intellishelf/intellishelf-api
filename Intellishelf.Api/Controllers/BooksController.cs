using Intellishelf.Api.Contracts.Books;
using Intellishelf.Api.Mappers.Books;
using Intellishelf.Api.Services;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Books.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[ApiController]
[Authorize]
[Route("books")]
public class BooksController(IBooksMapper mapper, AiServiceOld aiService, IBookService bookService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Book>>> GetBooks()
    {
        var result = await bookService.TryGetBooksAsync(CurrentUserId);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<string>> AddBook([FromBody] AddBookRequestContract contract)
    {
        var id = await bookService.TryAddBookAsync(mapper.MapAdd(CurrentUserId, contract));

        return CreatedAtAction(nameof(GetBooks), new { id }, id);
    }

    [HttpDelete("{bookId}")]
    public async Task<IActionResult> DeleteBook(string bookId)
    {
        await bookService.TryDeleteBookAsync(mapper.MapDelete(CurrentUserId, bookId));

        return NoContent();
    }

    [HttpPost("parse-image")]
    public async Task<ActionResult<ParsedBookResponseContract>> ParseImage([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        using (var stream = file.OpenReadStream())
        {
            var result = await aiService.ParseBookAsync(stream);
            return Ok(result);
        }
    }

    [HttpPost("parse-text")]
    public async Task<ActionResult<ParsedBookResponseContract>> ParseText([FromBody] ParseFromTextContract contract)
    {
        var result = await aiService.ParseBookFromTextAsync(contract.Text);
        return Ok(result);
    }
}