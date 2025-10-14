using Intellishelf.Common.TryResult;

namespace Intellishelf.Domain.Files.Services;

public interface IFileStorageService
{
    Task<TryResult<string>> UploadFileAsync(string userId, Stream fileStream, string fileName);
    Task<TryResult<bool>> DeleteFileFromUrlAsync(string url);
}
