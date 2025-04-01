using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

public class ApiControllerBase : ControllerBase
{
    protected string CurrentUserId =>  User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
}