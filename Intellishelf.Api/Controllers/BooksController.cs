using Intellishelf.Api.Models.Dtos;
using Intellishelf.Api.Services;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Books.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[ApiController]
[Route("books")]
public class BooksController(AiService aiService, IBookService bookService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<Book>>> GetBooks()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (userId == null)
            return Unauthorized();

        var books = await bookService.GetBooksAsync(userId);
        return Ok(books.Value);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<string>> AddBook([FromBody] AddBook request)
    {
        var userId = User.FindFirst("userId")?.Value;
        if (userId == null)
            return Unauthorized();

        var id = await bookService.AddBookAsync(userId, request);
        return CreatedAtAction(nameof(GetBooks), new { id = id }, id);
    }

    [HttpDelete("{bookId}")]
    [Authorize]
    public async Task<IActionResult> DeleteBook(string bookId)
    {
        var userId = User.FindFirst("userId")?.Value;
        if (userId == null)
            return Unauthorized();

        await bookService.DeleteBookAsync(userId, bookId);
        return NoContent();
    }

    [HttpPost("parse-image")]
    [Authorize]
    public async Task<ActionResult<ParsedBookResponse>> ParseImage([FromForm] IFormFile file)
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
    [Authorize]
    public async Task<ActionResult<ParsedBookResponse>> ParseText([FromBody] ParseTextRequest request)
    {
        var result = await aiService.ParseBookFromTextAsync(request.Text);
        return Ok(result);
    }
}