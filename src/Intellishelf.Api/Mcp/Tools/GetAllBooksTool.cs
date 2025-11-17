using System.ComponentModel;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Chat.Models;
using ModelContextProtocol;

namespace Intellishelf.Api.Mcp.Tools;

[McpServerToolType]
public class GetAllBooksTool(IBookDao bookDao)
{
    [McpServerTool]
    [Description("Get all books from the user's library. Returns the complete collection with title, authors, publication info, reading status, and tags.")]
    public async Task<List<BookChatContext>> GetAllBooks(
        [Description("The user ID whose books to retrieve")]
        string userId)
    {
        var booksResult = await bookDao.GetBooksAsync(userId);

        if (!booksResult.IsSuccess)
            return [];

        // Project to lightweight context (exclude heavy properties like image URLs)
        return booksResult.Value.Select(b => new BookChatContext
        {
            Id = b.Id,
            Title = b.Title,
            Authors = b.Authors,
            Publisher = b.Publisher,
            PublicationDate = b.PublicationDate,
            Pages = b.Pages,
            Isbn10 = b.Isbn10,
            Isbn13 = b.Isbn13,
            Status = b.Status.ToString(),
            StartedReadingDate = b.StartedReadingDate,
            FinishedReadingDate = b.FinishedReadingDate,
            Tags = b.Tags
        }).ToList();
    }
}
