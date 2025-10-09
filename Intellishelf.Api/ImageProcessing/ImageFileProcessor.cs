using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace Intellishelf.Api.ImageProcessing;

public interface IImageFileProcessor
{
    Task<Stream> ProcessAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public sealed class ImageFileProcessor : IImageFileProcessor
{
    private static readonly Size MaxSize = new(1000, 1000);
    private const int DefaultJpegQuality = 85;

    public async Task<Stream> ProcessAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var sourceStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(sourceStream, cancellationToken);

        if (image.Width > MaxSize.Width || image.Height > MaxSize.Height)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = MaxSize,
                Sampler = KnownResamplers.Lanczos3
            }));
        }

        var output = new MemoryStream();
        await image.SaveAsync(output, GetEncoder(file.FileName), cancellationToken);
        output.Position = 0;
        return output;
    }

    private static IImageEncoder GetEncoder(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension?.ToLowerInvariant() switch
        {
            ".png" => new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.Level6
            },
            _ => new JpegEncoder
            {
                Quality = DefaultJpegQuality
            }
        };
    }
}