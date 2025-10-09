using Intellishelf.Api.ImageProcessing;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Intellishelf.Unit.Tests.ImageProcessing;

public class ImageFileProcessorTests
{
    private readonly IImageFileProcessor _processor = new ImageFileProcessor();

    [Fact]
    public async Task ProcessAsync_WithLargeJpeg_ResizesLongestSideToOneThousandPixels()
    {
        var jpegBytes = CreateImageBytes(2000, 1500, new JpegEncoder { Quality = 90 });
        using var inputStream = new MemoryStream(jpegBytes);
        var formFile = CreateFormFile(inputStream, jpegBytes.Length, "image/jpeg", "cover.jpg");

        await using var processedStream = await _processor.ProcessAsync(formFile);
        processedStream.Seek(0, SeekOrigin.Begin);

        using var processedImage = await Image.LoadAsync<Rgba32>(processedStream);
        Assert.Equal(1000, processedImage.Width);
        Assert.Equal(750, processedImage.Height);
    }

    [Fact]
    public async Task ProcessAsync_WithPng_KeepsFormatAndDimensions()
    {
        var pngBytes = CreateImageBytes(640, 480, new PngEncoder());
        using var inputStream = new MemoryStream(pngBytes);
        var formFile = CreateFormFile(inputStream, pngBytes.Length, "image/png", "cover.png");

        await using var processedStream = await _processor.ProcessAsync(formFile);
        processedStream.Seek(0, SeekOrigin.Begin);

        using var processedImage = await Image.LoadAsync<Rgba32>(processedStream);
        Assert.Equal(640, processedImage.Width);
        Assert.Equal(480, processedImage.Height);
        Assert.Same(PngFormat.Instance, processedImage.Metadata.DecodedImageFormat);
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

    private static byte[] CreateImageBytes(int width, int height, IImageEncoder encoder)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(100, 150, 200));
        using var stream = new MemoryStream();
        image.Save(stream, encoder);
        return stream.ToArray();
    }
}