namespace Intellishelf.Domain.Books.Helpers;

public static class IsbnHelper
{
    public static bool IsValidIsbn(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return false;

        // Remove hyphens and spaces for validation
        var cleanIsbn = isbn.Replace("-", "").Replace(" ", "").Trim();

        // ISBN must be 10 or 13 characters
        if (cleanIsbn.Length != 10 && cleanIsbn.Length != 13)
            return false;

        // All characters must be digits, except last char in ISBN-10 can be 'X'
        for (int i = 0; i < cleanIsbn.Length; i++)
        {
            char c = cleanIsbn[i];
            bool isLastChar = i == cleanIsbn.Length - 1;
            bool isIsbn10 = cleanIsbn.Length == 10;

            if (!char.IsDigit(c))
            {
                // Only allow 'X' or 'x' as last character of ISBN-10
                if (isIsbn10 && isLastChar && (c == 'X' || c == 'x'))
                    continue;

                return false;
            }
        }

        return true;
    }

    public static string NormalizeIsbn(string isbn)
    {
        return isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
    }

    public static string? ConvertIsbn10ToIsbn13(string isbn10)
    {
        if (string.IsNullOrWhiteSpace(isbn10))
            return null;

        var cleanIsbn = NormalizeIsbn(isbn10);
        if (cleanIsbn.Length != 10)
            return null;

        // ISBN-13 = 978 + first 9 digits of ISBN-10 + new check digit
        string isbn13Base = "978" + cleanIsbn[..9];

        // Calculate check digit for ISBN-13
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = isbn13Base[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        return isbn13Base + checkDigit;
    }
}
