using System.ComponentModel.DataAnnotations;

namespace Intellishelf.Api.Configuration;

public class DatabaseConfig
{
    public const string SectionName = "Database";

    [Required]
    public required string ConnectionString { get; init; }

    [Required]
    public required string DatabaseName { get; init; }
}
