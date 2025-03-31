using System.Text.Json;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Ai.Errors;
using Intellishelf.Domain.Books.Models;
using OpenAI.Chat;

namespace Intellishelf.Domain.Ai.Services;

public class AiService(ChatClient chatClient) : IAiService
{
    private const string Prompt =
        "You are a book page parser. You are given a verso book page which may include all information about book. I want add this to my personal library. Extract content language, title, author, publisher, publicationYear, pages, isbn, description. Language of content may be any so find out yourself. Don't translate content, don't rephrase content, just parse. If you can't determine property, set null instead of improvising";

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
                                                     "author": {
                                                         "type": ["string", "null"]
                                                     },
                                                     "publicationYear": {
                                                         "type": ["number", "null"]
                                                     },
                                                     "publisher": {
                                                         "type": ["string", "null"]
                                                     },
                                                     "isbn": {
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
                                                     "title", "author","publicationYear","publisher","isbn","pages", "description"
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
                Author = "Author Name",
                Publisher = "Sample Publisher",
                PublicationYear = DateTime.UtcNow.Year,
                Pages = 100,
                Isbn = "1234567890",
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
}