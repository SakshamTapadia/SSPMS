using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/badges")]
[Authorize]
public class BadgesController : BaseController
{
    private readonly IGamificationService _gamification;
    public BadgesController(IGamificationService gamification) => _gamification = gamification;

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll() => Ok(await _gamification.GetAllBadgesAsync());

    [HttpGet("me"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyBadges() => Ok(await _gamification.GetEmployeeBadgesAsync(CurrentUserId));

    [HttpGet("user/{id:guid}"), Authorize(Roles = "Trainer,Admin")]
    public async Task<IActionResult> GetUserBadges(Guid id) => Ok(await _gamification.GetEmployeeBadgesAsync(id));

    [HttpGet("xp/me"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyXP() => Ok(await _gamification.GetXPSummaryAsync(CurrentUserId));

    [HttpGet("dashboard/me"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyDashboard() => Ok(await _gamification.GetEmployeeDashboardAsync(CurrentUserId));
}
