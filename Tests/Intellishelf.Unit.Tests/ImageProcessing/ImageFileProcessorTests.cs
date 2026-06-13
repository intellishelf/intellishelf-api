using Intellishelf.Api.ImageProcessing;
using Microsoft.AspNetCore.Http;
using SkiaSharp;
using Xunit;

namespace Intellishelf.Unit.Tests.ImageProcessing;

public class ImageFileProcessorTests
{
    private readonly IImageFileProcessor _processor = new ImageFileProcessor();

    [Fact]
    public async Task ProcessAsync_WithLargeJpeg_ResizesLongestSideToOneThousandPixels()
    {
        var jpegBytes = CreateImageBytes(2000, 1500, SKEncodedImageFormat.Jpeg, 90);
        using var inputStream = new MemoryStream(jpegBytes);
        var formFile = CreateFormFile(inputStream, jpegBytes.Length, "image/jpeg", "cover.jpg");

        await using var processedStream = await _processor.ProcessAsync(formFile);
        processedStream.Seek(0, SeekOrigin.Begin);

        using var processedImage = SKBitmap.Decode(processedStream);
        Assert.Equal(1000, processedImage.Width);
        Assert.Equal(750, processedImage.Height);
    }

    [Fact]
    public async Task ProcessAsync_WithPng_KeepsFormatAndDimensions()
    {
        var pngBytes = CreateImageBytes(640, 480, SKEncodedImageFormat.Png, 100);
        using var inputStream = new MemoryStream(pngBytes);
        var formFile = CreateFormFile(inputStream, pngBytes.Length, "image/png", "cover.png");

        await using var processedStream = await _processor.ProcessAsync(formFile);
        processedStream.Seek(0, SeekOrigin.Begin);

        using var processedCodec = SKCodec.Create(processedStream);
        var processedImage = processedCodec.Info;
        Assert.Equal(640, processedImage.Width);
        Assert.Equal(480, processedImage.Height);
        Assert.Equal(SKEncodedImageFormat.Png, processedCodec.EncodedFormat);
    }

    private static IFormFile CreateFormFile(Stream stream, long length, string contentType, string fileName)
    {
        var formFile = new FormFile(stream, 0, length, "ImageFile", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

        return formFile;
    }

    private static byte[] CreateImageBytes(int width, int height, SKEncodedImageFormat format, int quality)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(new SKColor(100, 150, 200));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality);
        using var stream = new MemoryStream();
        data.SaveTo(stream);
        return stream.ToArray();
    }
}
