using Intellishelf.Common.TryResult;

namespace Intellishelf.Domain.Books;

/// <summary>
/// Service for fetching book metadata from external APIs (Google Books, Amazon).
/// Implements fallback strategy: Google Books (primary) → Amazon (fallback).
/// </summary>
public interface IIsbnLookupService
{
    /// <summary>
    /// Looks up book metadata by ISBN from external APIs.
    /// Tries Google Books first, falls back to Amazon if not found.
    /// </summary>
    /// <param name="isbn">Normalized ISBN (10 or 13 digits, no hyphens)</param>
    /// <returns>
    /// Success: BookMetadata with source tag
    /// Failure: BookNotFound, ExternalApiFailure errors
    /// </returns>
    Task<TryResult<BookMetadata>> LookupByIsbnAsync(string isbn);
}
