using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Intellishelf.Api.Configuration;
using Intellishelf.Api.Contracts.Books;

namespace Intellishelf.Api.Services;

public class AiServiceOld(HttpClient httpClient, IOptions<AiConfig> aiOptions)
{
    public const string Prompt  = "You are a book page parser. You are given a verso book page which may include all information about book. I want add this to my personal library. Extract content language, title, authors, publisher, publicationDate, pages, isbn, description. Return as JSON with double quotes. Language of content may be any so find out yourself. Don't translate content, don't rephrase content, just parse. Example: {\"language\": \"ua\", \"title\": \"Castle\", \"authors\": \"Franz Kafka\"}";

    private readonly string _azureVisionEndpoint = aiOptions.Value.AzureVisionEndpoint;
    private readonly string _azureVisionKey = aiOptions.Value.AzureVisionKey;
    private readonly string _openAiApiKey = aiOptions.Value.OpenAiApiKey;

    public async Task<ParsedBookResponseContract> ParseBookAsync(Stream imageStream)
    {
        // Call Azure Vision API
        using (var content = new StreamContent(imageStream))
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            var requestUrl = $"{_azureVisionEndpoint}/imageanalysis:analyze?features=Read";
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _azureVisionKey);
            var response = await httpClient.PostAsync(requestUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            // In a real implementation, parse the response JSON to extract text lines.
            var simulatedOcrText = "Simulated OCR text from image";

            // Now call OpenAI API with the OCR text.
            return await ParseBookFromTextAsync(simulatedOcrText);
        }
    }

    public async Task<ParsedBookResponseContract> ParseBookFromTextAsync(string text)
    {
        // Prepare OpenAI request payload
        var requestBody = new
        {
            model = "gpt-4o-mini",
            temperature = 0,
            messages = new object[]
            {
                    new { role = "user", content = text },
                    new { role = "system", content = Prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        requestMessage.Headers.Add("Authorization", $"Bearer {_openAiApiKey}");
        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        // Parse the response JSON to extract the parsed book details.
        // For demonstration purposes, we simulate a parsed result:
        return new ParsedBookResponseContract
        {
            Title = "Sample Book",
            Authors = "Author Name",
            Publisher = "Sample Publisher",
            PublicationDate = DateTime.UtcNow,
            Pages = 100,
            Isbn = "1234567890",
            Description = "Sample description",
            Language = "en"
        };
    }
}