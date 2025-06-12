using Azure.Storage.Blobs;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Files.ErrorCodes;

namespace Intellishelf.Domain.Files.Services;

public class FileStorageService(BlobContainerClient containerClient) : IFileStorageService
{
    public async Task<TryResult<string>> UploadFileAsync(string userId, Stream fileStream, string originalFileName)
    {
        // Ensure the container exists (create if it doesn't)
        await containerClient.CreateIfNotExistsAsync();
 
        // Generate a unique filename inside the service
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
        var blobClient = containerClient.GetBlobClient(GetUserFilePath(userId, uniqueFileName));
 
        await blobClient.UploadAsync(fileStream, true);
 
        return blobClient.Uri.ToString();
    }

    public async Task<TryResult<bool>> DeleteFileFromUrlAsync(string url)
    {
        try
        {
            var blobPath = ExtractBlobPathFromUrl(url);
            var blobClient = containerClient.GetBlobClient(blobPath);
            var deleteResult = await blobClient.DeleteIfExistsAsync();
            return deleteResult.Value;
        }
        catch (Exception)
        {
            return new Error(FileErrorCodes.DownloadingFailed, "The file couldn't be deleted");
        }
    }

    private string ExtractBlobPathFromUrl(string url)
    {
        var uri = new Uri(url);
        // Extract path after container name
        // URL format: https://storage.blob.core.windows.net/container/userFiles/userId/filename.jpg
        var segments = uri.AbsolutePath.TrimStart('/').Split('/');
        return string.Join("/", segments.Skip(1)); // Skip container name
    }

    private static string GetUserFilePath(string userId, string fileName) => $"userFiles/{userId}/{fileName}";
}
