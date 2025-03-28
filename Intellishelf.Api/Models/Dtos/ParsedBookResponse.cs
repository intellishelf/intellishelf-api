namespace Intellishelf.Api.Models.Dtos;

public class ParsedBookResponse
{
    public string Language { get; set; }
    public string Title { get; set; }
    public string Authors { get; set; }
    public string Publisher { get; set; }
    public DateTime PublicationDate { get; set; }
    public int Pages { get; set; }
    public string ISBN { get; set; }
    public string Description { get; set; }
}