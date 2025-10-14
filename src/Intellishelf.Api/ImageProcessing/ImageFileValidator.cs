using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Files.ErrorCodes;

namespace Intellishelf.Api.ImageProcessing;

public interface IImageFileValidator
{
    TryResult Validate(IFormFile file);
}

public sealed class ImageFileValidator : IImageFileValidator
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    public TryResult Validate(IFormFile file)
    {
        if (file.Length == 0)
        {
            return new Error(FileErrorCodes.InvalidFileType, "Cover image cannot be empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return new Error(FileErrorCodes.FileTooLarge, "Cover image must be 10 MB or smaller.");
        }

        var contentTypeAllowed = !string.IsNullOrWhiteSpace(file.ContentType) && AllowedContentTypes.Contains(file.ContentType);
        var fileExtension = Path.GetExtension(file.FileName);
        var extensionAllowed = !string.IsNullOrWhiteSpace(fileExtension) && AllowedExtensions.Contains(fileExtension);

        if (!contentTypeAllowed || !extensionAllowed)
        {
            return new Error(FileErrorCodes.InvalidFileType, $"Only images are supported.");
        }

        return TryResult.Success();
    }
}