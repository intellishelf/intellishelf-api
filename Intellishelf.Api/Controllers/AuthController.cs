using System.Security.Claims;
using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Api.Mappers.Users;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Domain.Users.Models;
using Intellishelf.Domain.Users.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;


namespace Intellishelf.Api.Controllers;

[ApiController]
[Authorize]
[Route("auth")]
public class AuthController(
    IUserMapper mapper,
    IAuthService authService,
    IOptions<AuthConfig> authOptions) : ApiControllerBase
{
    private readonly IUserMapper _mapper = mapper;
    private readonly IAuthService _authService = authService;
    private readonly AuthConfig _authConfig = authOptions.Value;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultContract>> Login([FromBody] LoginRequestContract loginRequest)
    {
        var result = await _authService.TrySignInAsync(_mapper.MapLoginRequest(loginRequest));

        if (!result.IsSuccess)
            return HandleErrorResponse(result.Error);

        SetRefreshCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);

        return Ok(_mapper.MapLoginResult(result.Value));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterUserRequestContract registerRequest)
    {
        var result = await _authService.TryRegisterAsync(_mapper.MapRegisterUserRequest(registerRequest));

        if (!result.IsSuccess)
            return HandleErrorResponse(result.Error);

        SetRefreshCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);

        return Ok(_mapper.MapLoginResult(result.Value));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultContract>> RefreshToken([FromBody] RefreshTokenRequestContract? request)
    {
        var refreshToken = Request.Cookies[_authConfig.RefreshTokenCookieName]
            ?? request?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized();

        var result = await _authService.TryRefreshTokenAsync(new RefreshTokenRequest(refreshToken));

        if (!result.IsSuccess)
        {
            ClearRefreshCookie();
            return HandleErrorResponse(result.Error);
        }

        SetRefreshCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);

        return Ok(_mapper.MapLoginResult(result.Value));
    }

    [HttpPost("revoke")]
    public async Task<ActionResult> RevokeToken([FromBody] RefreshTokenRequestContract request)
    {
        var result = await _authService.TryRevokeRefreshTokenAsync(_mapper.MapRefreshTokenRequest(request));

        return !result.IsSuccess
            ? HandleErrorResponse(result.Error)
            : Ok();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[_authConfig.RefreshTokenCookieName];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            ClearRefreshCookie();
            return NoContent();
        }

        var result = await _authService.TryRevokeRefreshTokenAsync(new RefreshTokenRequest(refreshToken));

        ClearRefreshCookie();

        return result.IsSuccess
            ? NoContent()
            : HandleErrorResponse(result.Error);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponseContract>> Me()
    {
        var result = await _authService.TryFindByIdAsync(CurrentUserId);

        return result.IsSuccess
            ? Ok(_mapper.MapUser(result.Value))
            : HandleErrorResponse(result.Error);
    }

    [HttpPost("google/exchange")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultContract>> ExchangeGoogleCode([FromBody] ExchangeCodeRequest request)
    {
        var result = await _authService.TryExchangeGoogleCodeAsync(request);

        if (!result.IsSuccess)
            return HandleErrorResponse(result.Error);

        SetRefreshCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);

        return Ok(_mapper.MapLoginResult(result.Value));
    }

    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult SignInWithGoogle([FromQuery] string? returnUrl = null)
    {
        var redirectUri = Url.Action(nameof(FinalizeGoogleLogin), "Auth", new { returnUrl })
            ?? "/api/auth/finalize";

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("finalize")]
    [AllowAnonymous]
    public async Task<IActionResult> FinalizeGoogleLogin([FromQuery] string? returnUrl = null)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(AuthConfig.ExternalCookieScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            ClearRefreshCookie();
            return Unauthorized();
        }

        var principal = authenticateResult.Principal;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var externalId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? principal.FindFirstValue("sub");

        await HttpContext.SignOutAsync(AuthConfig.ExternalCookieScheme);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(externalId))
        {
            ClearRefreshCookie();
            return Unauthorized();
        }

        var result = await _authService.TrySignInExternalAsync(
            new ExternalLoginRequest(email, externalId, AuthProvider.Google));

        if (!result.IsSuccess)
        {
            ClearRefreshCookie();
            return HandleErrorResponse(result.Error);
        }

        SetRefreshCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Ok(new ExternalLoginResultContract(result.Value.AccessToken, result.Value.AccessTokenExpiry));
    }

    private void SetRefreshCookie(string refreshToken, DateTime expiry)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = _authConfig.RefreshTokenCookieSecure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true
        };

        var expiresUtc = DateTime.SpecifyKind(expiry, DateTimeKind.Utc);
        options.Expires = new DateTimeOffset(expiresUtc);

        if (!string.IsNullOrEmpty(_authConfig.RefreshTokenCookieDomain))
            options.Domain = _authConfig.RefreshTokenCookieDomain;

        Response.Cookies.Append(_authConfig.RefreshTokenCookieName, refreshToken, options);
    }

    private void ClearRefreshCookie()
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = _authConfig.RefreshTokenCookieSecure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UnixEpoch,
            IsEssential = true
        };

        if (!string.IsNullOrEmpty(_authConfig.RefreshTokenCookieDomain))
            options.Domain = _authConfig.RefreshTokenCookieDomain;

        Response.Cookies.Append(_authConfig.RefreshTokenCookieName, string.Empty, options);
    }
}