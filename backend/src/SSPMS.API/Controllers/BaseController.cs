using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SSPMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role)!;
    protected string CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email)!;
    protected string CurrentUserIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
