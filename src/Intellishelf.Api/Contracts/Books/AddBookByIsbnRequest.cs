namespace Intellishelf.Api.Contracts.Books;

/// <summary>
/// Request contract for adding a book by ISBN lookup.
/// </summary>
public sealed class AddBookByIsbnRequest
{
    /// <summary>
    /// The ISBN-10 or ISBN-13 to lookup. May contain hyphens or spaces.
    /// </summary>
    public required string Isbn { get; init; }
}
