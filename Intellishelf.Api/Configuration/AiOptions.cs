 namespace Intellishelf.Api.Configuration
{
    public class AiOptions
    {
        public const string SectionName = "Ai";
        
        public required string AzureVisionEndpoint { get; init; }
        public required string AzureVisionKey { get; init; }
        public required string OpenAiApiKey { get; init; }
        public string Prompt { get; init; } = "You are a book page parser. You are given a verso book page which may include all information about book. I want add this to my personal library. Extract content language, title, authors, publisher, publicationDate, pages, isbn, description. Return as JSON with double quotes. Language of content may be any so find out yourself. Don't translate content, don't rephrase content, just parse. Example: {\"language\": \"ua\", \"title\": \"Castle\", \"authors\": \"Franz Kafka\"}";
    }
}