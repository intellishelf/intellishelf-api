using Intellishelf.Api.Contracts.Books;
using Intellishelf.Api.Mappers.Books;
using Xunit;

namespace Intellishelf.Unit.Tests.Books;

public class BookMapperLanguageTests
{
    private readonly BookMapper _mapper = new();

    [Theory]
    [InlineData("uk", "uk")]
    [InlineData("  en  ", "en")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void MapAdd_NormalizesLanguage(string? input, string? expected)
    {
        var request = _mapper.MapAdd("user", Contract(input), null);

        Assert.Equal(expected, request.Language);
    }

    [Theory]
    [InlineData("uk", "uk")]
    [InlineData("  en  ", "en")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void MapUpdate_NormalizesLanguage(string? input, string? expected)
    {
        var request = _mapper.MapUpdate("user", "book", Contract(input), null);

        Assert.Equal(expected, request.Language);
    }

    private static BookRequestContractBase Contract(string? language) =>
        new() { Title = "Title", Language = language };
}
