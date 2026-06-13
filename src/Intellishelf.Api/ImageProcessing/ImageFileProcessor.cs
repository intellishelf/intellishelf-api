using SkiaSharp;

namespace Intellishelf.Api.ImageProcessing;

public interface IImageFileProcessor
{
    Task<Stream> ProcessAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public sealed class ImageFileProcessor : IImageFileProcessor
{
    private const int MaxDimension = 1000;
    private const int DefaultJpegQuality = 85;
    private const int DefaultPngQuality = 100;

    public async Task<Stream> ProcessAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var sourceStream = file.OpenReadStream();
        using var sourceBitmap = SKBitmap.Decode(sourceStream)
            ?? throw new InvalidOperationException("The uploaded image could not be decoded.");

        cancellationToken.ThrowIfCancellationRequested();

        using var processedBitmap = ResizeIfNeeded(sourceBitmap);
        using var processedImage = SKImage.FromBitmap(processedBitmap);
        using var encodedImage = processedImage.Encode(GetEncodedImageFormat(file.FileName), GetQuality(file.FileName))
            ?? throw new InvalidOperationException("The uploaded image could not be encoded.");
        var output = new MemoryStream();
        encodedImage.SaveTo(output);
        output.Position = 0;
        return output;
    }

    private static SKBitmap ResizeIfNeeded(SKBitmap sourceBitmap)
    {
        if (sourceBitmap.Width <= MaxDimension && sourceBitmap.Height <= MaxDimension)
        {
            return sourceBitmap.Copy();
        }

        var scale = Math.Min(MaxDimension / (double)sourceBitmap.Width, MaxDimension / (double)sourceBitmap.Height);
        var targetWidth = Math.Max(1, (int)Math.Round(sourceBitmap.Width * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(sourceBitmap.Height * scale));
        var resizedBitmap = new SKBitmap(new SKImageInfo(targetWidth, targetHeight, sourceBitmap.ColorType, sourceBitmap.AlphaType));

        if (!sourceBitmap.ScalePixels(resizedBitmap, new SKSamplingOptions(SKCubicResampler.CatmullRom)))
        {
            resizedBitmap.Dispose();
            throw new InvalidOperationException("The uploaded image could not be resized.");
        }

        return resizedBitmap;
    }

    private static SKEncodedImageFormat GetEncodedImageFormat(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension?.ToLowerInvariant() == ".png"
            ? SKEncodedImageFormat.Png
            : SKEncodedImageFormat.Jpeg;
    }

    private static int GetQuality(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension?.ToLowerInvariant() switch
        {
            ".png" => DefaultPngQuality,
            _ => DefaultJpegQuality
        };
    }
}
