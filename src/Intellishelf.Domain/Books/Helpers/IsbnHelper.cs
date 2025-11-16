namespace Intellishelf.Domain.Books.Helpers;

public static class IsbnHelper
{
    public static bool IsValidIsbn(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return false;

        var cleanIsbn = NormalizeIsbn(isbn);

        // Just check length - 10 or 13 characters
        return cleanIsbn.Length == 10 || cleanIsbn.Length == 13;
    }

    public static string NormalizeIsbn(string isbn)
    {
        return isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
    }
}
