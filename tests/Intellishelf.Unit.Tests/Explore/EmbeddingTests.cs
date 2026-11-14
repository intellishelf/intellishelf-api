using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Models;
using Intellishelf.Domain.Explore;
using Intellishelf.Domain.Users.DataAccess;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Intellishelf.Unit.Tests.Explore;

public class EmbeddingTests
{
    private static AddBookRequest Book(string title = "Title", string[]? authors = null, string? description = null,
        string? annotation = null, string[]? tags = null, string? publisher = null, int? pages = null) => new()
        {
            UserId = "user",
            Title = title,
            Authors = authors,
            Description = description,
            Annotation = annotation,
            Tags = tags,
            Publisher = publisher,
            Pages = pages,
            Status = ReadingStatus.Unread
        };

    [Fact]
    public void IndexedTextHashTracksOnlyIndexedContentAndVersion()
    {
        string Hash(AddBookRequest b, string model = "model", int dimensions = 1536) =>
            IndexedBookText.Hash(IndexedBookText.Build(b, 12000), model, dimensions);
        var original = Hash(Book());
        Assert.Equal(original, Hash(Book(pages: 100)));
        Assert.NotEqual(original, Hash(Book(title: "Changed")));
        Assert.NotEqual(original, Hash(Book(authors: ["Author"])));
        Assert.NotEqual(original, Hash(Book(description: "Description")));
        Assert.NotEqual(original, Hash(Book(annotation: "Annotation")));
        Assert.NotEqual(original, Hash(Book(tags: ["Tag"])));
        Assert.NotEqual(original, Hash(Book(publisher: "Publisher")));
        Assert.NotEqual(original, Hash(Book(), "other"));
        Assert.NotEqual(original, Hash(Book(), dimensions: 100));
        Assert.Equal(10, IndexedBookText.Build(Book(), 10).Length);
    }

    [Fact]
    public void IndexedTextRespectsByteAndCharacterLimitsWithoutSplittingUnicodeScalars()
    {
        var longUnicode = Book(description: string.Concat(Enumerable.Repeat("😀漢", 5000)));
        var limitedByBytes = IndexedBookText.Build(longUnicode, 12000);
        Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(limitedByBytes), 8188, 8191);
        Assert.DoesNotContain("�", limitedByBytes);
        Assert.True(limitedByBytes.Length < 12000);

        var text = IndexedBookText.Build(Book(title: "😀"), 8);
        Assert.Equal("Title: ", text);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(text) <= 8191);
        Assert.DoesNotContain("�", text);
        Assert.Equal("Title: 😀", IndexedBookText.Build(Book(title: "😀"), 9));
    }

    [Fact]
    public async Task FailedUserLookupAndMissingUserFailClosed()
    {
        var users = new Mock<IUserDao>();
        users.Setup(x => x.TryFindByIdAsync(It.IsAny<string>())).ReturnsAsync(new Error("Users.NotFound", "Missing"));
        var provider = new Mock<IEmbeddingProvider>();
        provider.SetupGet(x => x.ModelId).Returns("model");
        var indexer = new BookIndexer(users.Object, provider.Object, Options.Create(new ExploreOptions()));
        var result = await indexer.PrepareAsync(Book());
        Assert.Null(result.Embedding);
        provider.Verify(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void DefaultsAreValidAndInvalidDimensionsRejected()
    {
        var options = new ExploreOptions();
        Assert.True(options.IsValid());
        options.Dimensions = 3072;
        Assert.False(options.IsValid());
    }
}

public class ExploreRegistrationTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("Test", true)]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    public void StandInIsAllowedOnlyInDevelopmentAndTest(string environment, bool allowed)
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Explore:Provider"] = "StandIn" }).Build();
        var host = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        host.SetupGet(x => x.EnvironmentName).Returns(environment);
        using var provider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddExplore(configuration, host.Object).BuildServiceProvider();
        if (allowed)
            Assert.Equal("StandIn", provider.GetRequiredService<IOptions<ExploreOptions>>().Value.Provider);
        else
            Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<ExploreOptions>>().Value);
    }

    [Fact]
    public void AbsentSectionUsesDefaults()
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var host = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        host.SetupGet(x => x.EnvironmentName).Returns("Production");
        using var provider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddExplore(configuration, host.Object).BuildServiceProvider();
        Assert.True(provider.GetRequiredService<IOptions<ExploreOptions>>().Value.IsValid());
    }
}
