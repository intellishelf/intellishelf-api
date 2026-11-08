using Intellishelf.Data.Books.Entities;
using Intellishelf.Data.Indexes;
using MongoDB.Driver;
using Xunit;

namespace Intellishelf.Unit.Tests;

public class BookSearchIndexesTests
{
    [Theory]
    [InlineData(VectorSimilarity.Cosine, "cosine")]
    [InlineData(VectorSimilarity.Euclidean, "euclidean")]
    [InlineData(VectorSimilarity.DotProduct, "dotProduct")]
    public void VectorDefinition_UsesMongoDbSimilarityName(VectorSimilarity similarity, string expected)
    {
        var model = new CreateVectorSearchIndexModel<BookEntity>(
            book => book.Embedding!, "vector_index", similarity, 1536, book => book.UserId);

        var definition = BookSearchIndexes.VectorDefinition(model);

        Assert.Equal(expected, definition["fields"][0]["similarity"].AsString);
    }
}
