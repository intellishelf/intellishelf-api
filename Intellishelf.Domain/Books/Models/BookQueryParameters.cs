namespace Intellishelf.Domain.Books.Models;

public enum BookOrderBy
{
    Title,
    Author,
    Published,
    Added
}

public class BookQueryParameters
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 50;
    
    private int _pageSize = DefaultPageSize;
    
    public int Page { get; set; } = 1;
    
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
    
    public BookOrderBy OrderBy { get; set; } = BookOrderBy.Added;
    
    public bool Ascending { get; set; } = true;
}
