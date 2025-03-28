using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Api.Models.Dtos;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Auth.ErrorCodes;
using Intellishelf.Domain.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseContract>> Login([FromBody] LoginRequestContract loginRequest)
    {
        var result = await authService.SignInAsync(loginRequest.UserName, loginRequest.Password);

        if (!result.IsSuccess)
            return HandleErrorResponse(result.Error);

        return Ok(new LoginResponseContract(result.Value));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserContract>> Me()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (userId == null)
            return Unauthorized();

        var user = await authService.FindByIdAsync(userId);
        if (user.IsSuccess)
            return Ok(new UserContract(user.Value.UserId.ToString(), user.Value.UserName));

        return HandleErrorResponse(user.Error);
    }

    private ObjectResult HandleErrorResponse(Error error)
    {
        return error.Code switch
        {
            AuthErrorCodes.UserNotFound => NotFound(error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, error)
        };
    }
}