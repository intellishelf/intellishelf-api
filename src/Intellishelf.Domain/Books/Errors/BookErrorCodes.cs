namespace Intellishelf.Domain.Books.Errors;

public static class BookErrorCodes
{
    public const string BookNotFound = "Books.NotFound";
    public const string InvalidIsbn = "Books.InvalidIsbn";
    public const string DuplicateIsbn = "Books.DuplicateIsbn";
    public const string IsbnNotFound = "Books.IsbnNotFound";
    public const string MetadataServiceError = "Books.MetadataServiceError";
    public const string CoverImageDownloadFailed = "Books.CoverImageDownloadFailed";
}