using OpenAI.Embeddings;

namespace Intellishelf.Domain.Ai.Services;

public class EmbeddingService(EmbeddingClient embeddingClient) : IEmbeddingService
{
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var response = await embeddingClient.GenerateEmbeddingAsync(text);

            return response.Value.ToFloats().ToArray();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to generate embedding: {e.Message}", e);
        }
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts)
    {
        try
        {
            var response = await embeddingClient.GenerateEmbeddingsAsync(texts);

            return response.Value
                .Select(e => e.ToFloats().ToArray())
                .ToArray();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to generate embeddings: {e.Message}", e);
        }
    }
}
