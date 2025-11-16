using Intellishelf.Common.TryResult;

namespace Intellishelf.Domain.Files.Services;

public interface IHttpImageDownloader
{
    Task<TryResult<Stream>> DownloadImageAsync(string imageUrl);
}
