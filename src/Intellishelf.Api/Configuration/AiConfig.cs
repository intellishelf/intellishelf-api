 namespace Intellishelf.Api.Configuration
{
    public class AiConfig
    {
        public const string SectionName = "Ai";
        
        public required string AzureVisionEndpoint { get; init; }
        public required string AzureVisionKey { get; init; }
        public required string OpenAiApiKey { get; init; }
    }
}