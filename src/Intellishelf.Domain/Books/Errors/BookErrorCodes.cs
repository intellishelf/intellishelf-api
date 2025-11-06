namespace Intellishelf.Domain.Books.Errors;

public static class BookErrorCodes
{
    public const string BookNotFound = "Books.NotFound";
    public const string InvalidIsbn = "Books.InvalidIsbn";
    public const string DuplicateBook = "Books.DuplicateBook";
    public const string ExternalApiFailure = "Books.ExternalApiFailure";
}