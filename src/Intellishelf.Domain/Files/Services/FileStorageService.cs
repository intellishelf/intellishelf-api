using Azure.Storage.Blobs;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Files.ErrorCodes;

namespace Intellishelf.Domain.Files.Services;

public class FileStorageService(BlobContainerClient containerClient) : IFileStorageService
{
    public async Task<TryResult<string>> UploadFileAsync(string userId, Stream fileStream, string originalFileName)
    {
        try
        {
            // Ensure the container exists (create if it doesn't)
            await containerClient.CreateIfNotExistsAsync();
     
            // Generate a unique filename inside the service
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            var blobClient = containerClient.GetBlobClient(GetUserFilePath(userId, uniqueFileName));
     
            await blobClient.UploadAsync(fileStream, true);
     
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            return new Error(FileErrorCodes.UploadFailed, $"File upload failed: {ex.Message}");
        }
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
            return new Error(FileErrorCodes.DeletionFailed, "The file couldn't be deleted");
        }
    }

    public async Task<TryResult<int>> DeleteAllUserFilesAsync(string userId)
    {
        try
        {
            await containerClient.CreateIfNotExistsAsync();

            var prefix = $"userFiles/{userId}/";
            var deletedCount = 0;

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                try
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    await blobClient.DeleteIfExistsAsync();
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    // Log warning but continue - best effort deletion
                    System.Diagnostics.Debug.WriteLine($"Warning: Failed to delete blob {blobItem.Name}: {ex.Message}");
                }
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            return new Error(FileErrorCodes.DeletionFailed, $"Failed to delete user files: {ex.Message}");
        }
    }

    private string ExtractBlobPathFromUrl(string url)
    {
        var builder = new BlobUriBuilder(new Uri(url));
        return builder.BlobName;
    }

    private static string GetUserFilePath(string userId, string fileName) => $"userFiles/{userId}/{fileName}";
}
