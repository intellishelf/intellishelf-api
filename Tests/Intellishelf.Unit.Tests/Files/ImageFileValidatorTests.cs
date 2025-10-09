using System.IO;
using Intellishelf.Api.Services;
using Intellishelf.Api.Validators;
using Intellishelf.Domain.Files.ErrorCodes;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Intellishelf.Unit.Tests.Files;

public class ImageFileValidatorTests
{
    private const int ThreeMegabytes = 3 * 1024 * 1024;
    private readonly IImageFileValidator _validator = new ImageFileValidator();

    [Fact]
    public void Validate_WithValidJpegUnderLimit_ReturnsSuccess()
    {
        var file = CreateFormFile("cover.jpg", "image/jpeg", 1024);

        var result = _validator.Validate(file);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_WithUnsupportedContentType_ReturnsInvalidFileType()
    {
        var file = CreateFormFile("cover.pdf", "application/pdf", 1024);

        var result = _validator.Validate(file);

        Assert.False(result.IsSuccess);
        Assert.Equal(FileErrorCodes.InvalidFileType, result.Error?.Code);
    }

    [Fact]
    public void Validate_WithTooLargeFile_ReturnsFileTooLarge()
    {
        var file = CreateFormFile("cover.jpg", "image/jpeg", ThreeMegabytes + 1);

        var result = _validator.Validate(file);

        Assert.False(result.IsSuccess);
        Assert.Equal(FileErrorCodes.FileTooLarge, result.Error?.Code);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, int sizeInBytes)
    {
        var content = new byte[sizeInBytes];
        var stream = new MemoryStream(content);
        var formFile = new FormFile(stream, 0, sizeInBytes, "ImageFile", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

        return formFile;
    }
}
