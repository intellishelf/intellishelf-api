 namespace Intellishelf.Api.Configuration
{
    public class AzureConfig
    {
        public const string SectionName = "Azure";
        
        public required string StorageConnectionString { get; init; }
        public required string StorageContainer { get; init; }
    }
}