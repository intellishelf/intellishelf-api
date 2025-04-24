using Azure.Storage.Blobs;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Files.ErrorCodes;

namespace Intellishelf.Domain.Files.Services;

public class FileStorageService(BlobContainerClient containerClient) : IFileStorageService
{
    public async Task<TryResult<string>> UploadFileAsync(string userId, Stream fileStream, string fileName)
    {
        var blobClient = containerClient.GetBlobClient(GetUserFilePath(userId, fileName));

        await blobClient.UploadAsync(fileStream, true);

        return blobClient.Uri.ToString();
    }

    public async Task<TryResult<Stream>> DownloadFileAsync(string userId, string fileName)
    {
        try
        {
            var response = await containerClient
                .GetBlobClient(GetUserFilePath(userId, fileName))
                .DownloadAsync();

            return response.Value.Content;
        }
        catch (Exception e)
        {
            return new Error(FileErrorCodes.DownloadingFailed, "The user file couldn't be downloaded");
        }
    }

    public async Task<TryResult<bool>> DeleteFileAsync(string userId, string fileName)
    {
        var blobClient = containerClient.GetBlobClient(GetUserFilePath(userId, fileName));

        var deleteResult = await blobClient.DeleteIfExistsAsync();

        return deleteResult.Value;
    }

    public async Task<TryResult<Stream>> DownloadFileAsync(string fileName)
    {
        try
        {
            var response = await containerClient
                .GetBlobClient($"sharedFiles/{fileName}")
                .DownloadAsync();

            return response.Value.Content;
        }
        catch (Exception e)
        {
            return new Error(FileErrorCodes.DownloadingFailed, "The file couldn't be downloaded");
        }
    }

    private static string GetUserFilePath(string userId, string fileName) => $"userFiles/{userId}/{fileName}";
}