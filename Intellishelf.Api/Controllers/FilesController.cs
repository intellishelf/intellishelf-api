using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Files.Services;
using Intellishelf.Domain.Users.ErrorCodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[ApiController]
public class FilesController(IFileStorageService fileStorageService) : ApiControllerBase
{
    [HttpGet("users/{userId}/files/{fileName}")]
    [Authorize]
    public async Task<IActionResult> GetFile(string userId, string fileName)
    {
        if (CurrentUserId != userId)
            return HandleErrorResponse(new Error(UserErrorCodes.Unauthorized, "User is not allowed to read this file"));

        var result = await fileStorageService.DownloadFileAsync(CurrentUserId, fileName);

        return result.IsSuccess
            ? File(result.Value, "application/octet-stream", fileName)
            : HandleErrorResponse(result.Error);
    }

    [HttpGet("files/{fileName}")]
    public async Task<IActionResult> GetFile(string fileName)
    {
        var result = await fileStorageService.DownloadFileAsync(fileName);

        return result.IsSuccess
            ? File(result.Value, "application/octet-stream", fileName)
            : HandleErrorResponse(result.Error);    }
}