using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Api.Mappers.Auth;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Auth.ErrorCodes;
using Intellishelf.Domain.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[ApiController]
[Authorize]
[Route("auth")]
public class AuthController(IAuthMapper mapper, IAuthService authService) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseContract>> Login([FromBody] LoginRequestContract loginRequest)
    {
        var result = await authService.TrySignInAsync(mapper.Map(loginRequest));

        return !result.IsSuccess
            ? HandleErrorResponse(result.Error)
            : Ok(new LoginResponseContract(result.Value));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserContract>> Me()
    {
        var user = await authService.TryFindByIdAsync(CurrentUserId);

        return user.IsSuccess
            ? Ok(new UserContract(user.Value.UserId, user.Value.UserName))
            : HandleErrorResponse(user.Error);
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