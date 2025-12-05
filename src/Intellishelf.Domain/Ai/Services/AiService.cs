using System.Text;
using System.Text.Json;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Ai.Errors;
using Intellishelf.Domain.Books.Models;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Intellishelf.Domain.Ai.Services;

public class AiService(ChatClient chatClient, EmbeddingClient embeddingClient) : IAiService
{
    private const string Prompt =
        "You are a book page parser. You are given a verso book page which may include all information about book. I want to add it to my personal library. Extract content language, title, authors (via CSV if multiple), publisher, publicationYear, pages, isbn10, isbn13, description. Language of content may be any so find out yourself. Don't translate content, don't rephrase content, just parse. If you can't determine property, set null instead of improvising";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly ChatCompletionOptions ChatOptions = new()
    {
        Temperature = 0.0f,
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "book_info",
            jsonSchema: BinaryData.FromBytes("""
                                             {
                                                 "type": "object",
                                                 "properties": {
                                                     "title": {
                                                         "type": ["string", "null"]
                                                     },
                                                     "authors": {
                                                         "type": ["string", "null"]
                                                     },
                                                     "publicationYear": {
                                                         "type": ["number", "null"]
                                                     },
                                                     "publisher": {
                                                         "type": ["string", "null"]
                                                     },
                                                     "isbn10": {
                                                         "type": ["string", "null"]
                                                     },
                                                     "isbn13": {
                                                         "type": ["string", "null"]
                                                     },
                                                     "pages": {
                                                         "type": ["number", "null"]
                                                     },
                                                     "description": {
                                                         "type": ["string", "null"]
                                                     }
                                                 },
                                                 "required": [
                                                     "title", "authors","publicationYear","publisher","isbn10","isbn13","pages", "description"
                                                 ],
                                                 "additionalProperties": false
                                             }
                                             """u8.ToArray()),
            jsonSchemaIsStrict: true)
    };

    public async Task<TryResult<ParsedBook>> ParseBookFromTextAsync(string text, bool useMockedAi)
    {
        if (useMockedAi)
            return new ParsedBook
            {
                Title = "Sample Book",
                Authors = "Author Name",
                Publisher = "Sample Publisher",
                PublicationYear = DateTime.UtcNow.Year,
                Pages = 100,
                Isbn10 = "1234567890",
                Isbn13 = "1234567890123",
                Description = "Sample description"
            };

        List<ChatMessage> messages =
        [
            new SystemChatMessage(Prompt),
            new UserChatMessage(text)
        ];

        try
        {
            var response = await chatClient.CompleteChatAsync(messages, ChatOptions);

            var book = JsonSerializer.Deserialize<ParsedBook>(response.Value.Content[0].Text, JsonOptions);

            if (book == null)
                return new Error(AiErrorCodes.RequestFailed, "Response from AI could not be parsed.");

            return book;
        }
        catch (Exception e)
        {
            return new Error(AiErrorCodes.RequestFailed, e.Message);
        }
    }

    public async Task<TryResult<float[]>> GenerateEmbeddingAsync(Book book, bool useMockedAi = false)
    {
        if (useMockedAi)
        {
            // Return a mock embedding vector (3072 dimensions for text-embedding-3-large)
            var mockEmbedding = new float[3072];
            Array.Fill(mockEmbedding, 0.1f);
            return mockEmbedding;
        }

        try
        {
            var textToEmbed = FormatBookForEmbedding(book);
            var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(textToEmbed);
            var embedding = embeddingResponse.Value.ToFloats().ToArray();
            return embedding;
        }
        catch (Exception e)
        {
            return new Error(AiErrorCodes.RequestFailed, $"Failed to generate embedding: {e.Message}");
        }
    }

    private static string FormatBookForEmbedding(Book book)
    {
        var sb = new StringBuilder();
        sb.AppendLine(book.Title);
        sb.AppendLine($"Title: {book.Title}");

        if (!string.IsNullOrWhiteSpace(book.Authors))
            sb.AppendLine($"Author: {book.Authors}");

        // Note: The codebase doesn't have Genres field currently
        // If needed, this can be derived from Tags or added as a separate field

        if (book.PublicationDate.HasValue)
            sb.AppendLine($"Year of publication: {book.PublicationDate.Value.Year}");

        if (book.Tags is { Length: > 0 })
            sb.AppendLine($"Tags: {string.Join(", ", book.Tags)}");

        if (!string.IsNullOrWhiteSpace(book.Description))
            sb.AppendLine($"Description: {book.Description}");

        return sb.ToString();
    }
}
