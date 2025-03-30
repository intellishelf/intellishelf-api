using System.Text.Json;
using Microsoft.Extensions.Options;
using Intellishelf.Api.Configuration;
using Intellishelf.Api.Contracts.Books;
using Intellishelf.Common.TryResult;
using OpenAI.Chat;

namespace Intellishelf.Api.Services;

public class AiServiceOld(HttpClient httpClient, IOptions<AiConfig> aiConfig, ChatClient chatClient)
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

    private readonly AiConfig _config = aiConfig.Value;

    public async Task<TryResult<ParsedBookResponseContract>> ParseBookAsync(Stream imageStream)
    {
        // Call Azure Vision API
        using var content = new StreamContent(imageStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        var requestUrl = $"{_config.AzureVisionEndpoint}/imageanalysis:analyze?features=Read";
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _config.AzureVisionKey);
        var response = await httpClient.PostAsync(requestUrl, content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        // In a real implementation, parse the response JSON to extract text lines.
        var simulatedOcrText = "Simulated OCR text from image";

        // Now call OpenAI API with the OCR text.
        return await ParseBookFromTextAsync(simulatedOcrText);
    }

    public async Task<TryResult<ParsedBookResponseContract>> ParseBookFromTextAsync(string text)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(Prompt),
            new UserChatMessage(text)
        ];

        var response = await chatClient.CompleteChatAsync(messages, ChatOptions);

        var book = JsonSerializer.Deserialize<ParsedBookResponseContract>(response.Value.Content[0].Text, JsonOptions);

        if (book == null)
            return new Error("Ai.NoParsed", "Not parsed");

        return book;

        return new ParsedBookResponseContract
        {
            Title = "Sample Book",
            //Authors = ["Author Name"],
            Publisher = "Sample Publisher",
            PublicationYear = DateTime.UtcNow.Year,
            Pages = 100,
            Isbn = "1234567890",
            Description = "Sample description",
            Author = "Author Name",
        };
    }
}