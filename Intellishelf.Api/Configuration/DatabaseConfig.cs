namespace Intellishelf.Api.Configuration;

public class DatabaseConfig
{
    public const string SectionName = "Database";
    
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
}