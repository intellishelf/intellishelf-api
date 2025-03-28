using System.Text;
using System.Text.Json;
using Intellishelf.Api.Models.Dtos;
using Microsoft.Extensions.Options;
using Intellishelf.Api.Configuration;

namespace Intellishelf.Api.Services;

public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly string _azureVisionEndpoint;
    private readonly string _azureVisionKey;
    private readonly string _openAiApiKey;
    private readonly string _prompt;

    public AiService(HttpClient httpClient, IOptions<AiOptions> aiOptions)
    {
        _httpClient = httpClient;
        _azureVisionEndpoint = aiOptions.Value.AzureVisionEndpoint;
        _azureVisionKey = aiOptions.Value.AzureVisionKey;
        _openAiApiKey = aiOptions.Value.OpenAiApiKey;
        _prompt = aiOptions.Value.Prompt;
    }

    public async Task<ParsedBookResponse> ParseBookAsync(Stream imageStream)
    {
        // Call Azure Vision API
        using (var content = new StreamContent(imageStream))
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            var requestUrl = $"{_azureVisionEndpoint}/imageanalysis:analyze?features=Read";
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _azureVisionKey);
            var response = await _httpClient.PostAsync(requestUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            // In a real implementation, parse the response JSON to extract text lines.
            var simulatedOcrText = "Simulated OCR text from image";

            // Now call OpenAI API with the OCR text.
            return await ParseBookFromTextAsync(simulatedOcrText);
        }
    }

    public async Task<ParsedBookResponse> ParseBookFromTextAsync(string text)
    {
        // Prepare OpenAI request payload
        var requestBody = new
        {
            model = "gpt-4o-mini",
            temperature = 0,
            messages = new object[]
            {
                    new { role = "user", content = text },
                    new { role = "system", content = _prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        requestMessage.Headers.Add("Authorization", $"Bearer {_openAiApiKey}");
        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        // Parse the response JSON to extract the parsed book details.
        // For demonstration purposes, we simulate a parsed result:
        return new ParsedBookResponse
        {
            Title = "Sample Book",
            Authors = "Author Name",
            Publisher = "Sample Publisher",
            PublicationDate = DateTime.UtcNow,
            Pages = 100,
            ISBN = "1234567890",
            Description = "Sample description",
            Language = "en"
        };
    }
}