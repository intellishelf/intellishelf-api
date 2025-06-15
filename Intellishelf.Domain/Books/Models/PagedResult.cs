namespace Intellishelf.Domain.Books.Models;

public class PagedResult<T>(IReadOnlyCollection<T> items, int totalCount, int page, int pageSize)
{
    public IReadOnlyCollection<T> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public int TotalPages { get; } = (int)Math.Ceiling(totalCount / (double)pageSize);
}
