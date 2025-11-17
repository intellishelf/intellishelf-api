using System.ComponentModel;
using Intellishelf.Domain.Books.DataAccess;
using Intellishelf.Domain.Chat.Models;
using ModelContextProtocol;

namespace Intellishelf.Api.Mcp.Tools;

[McpServerToolType]
public class GetBooksByAuthorTool(IBookDao bookDao)
{
    [McpServerTool]
    [Description("Get books by a specific author from the user's library. Performs case-insensitive partial matching on author names.")]
    public async Task<List<BookChatContext>> GetBooksByAuthor(
        [Description("The user ID whose books to retrieve")]
        string userId,
        [Description("Author name to filter by (case-insensitive, partial match)")]
        string author)
    {
        var booksResult = await bookDao.GetBooksAsync(userId);

        if (!booksResult.IsSuccess)
            return [];

        // Filter books by author (case-insensitive partial match)
        var filteredBooks = booksResult.Value
            .Where(b => b.Authors != null &&
                       b.Authors.Contains(author, StringComparison.OrdinalIgnoreCase))
            .Select(b => new BookChatContext
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
            })
            .ToList();

        return filteredBooks;
    }
}
