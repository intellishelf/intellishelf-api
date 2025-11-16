using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Errors;

namespace Intellishelf.Domain.Files.Services;

public class HttpImageDownloader(HttpClient httpClient) : IHttpImageDownloader
{
    public async Task<TryResult<Stream>> DownloadImageAsync(string imageUrl)
    {
        try
        {
            var response = await httpClient.GetAsync(imageUrl);

            if (!response.IsSuccessStatusCode)
            {
                return new Error(
                    BookErrorCodes.CoverImageDownloadFailed,
                    $"Failed to download image. Status code: {response.StatusCode}");
            }

            var stream = await response.Content.ReadAsStreamAsync();
            return stream;
        }
        catch (HttpRequestException ex)
        {
            return new Error(
                BookErrorCodes.CoverImageDownloadFailed,
                $"Failed to download image: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new Error(
                BookErrorCodes.CoverImageDownloadFailed,
                $"Unexpected error downloading image: {ex.Message}");
        }
    }
}
