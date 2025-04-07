namespace Intellishelf.Domain.Books.Models;

public class UpdateBookRequest : BookRequestBase
{
    public required string Id { get; init; }
}