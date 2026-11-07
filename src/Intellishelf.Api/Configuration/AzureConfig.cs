using System.ComponentModel.DataAnnotations;

namespace Intellishelf.Api.Configuration
{
    public class AzureConfig
    {
        public const string SectionName = "Azure";

        [Required]
        public required string StorageConnectionString { get; init; }

        [Required]
        public required string StorageContainer { get; init; }
    }
}
