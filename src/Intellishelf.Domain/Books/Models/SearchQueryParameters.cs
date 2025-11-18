namespace Intellishelf.Domain.Books.Models;

public class SearchQueryParameters
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 50;

    private int _pageSize = DefaultPageSize;

    public required string SearchTerm { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public ReadingStatus? Status { get; init; }

    public float[]? SearchEmbedding { get; init; }
}