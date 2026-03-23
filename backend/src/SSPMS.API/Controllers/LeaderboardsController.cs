using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/leaderboards")]
[Authorize]
public class LeaderboardsController : BaseController
{
    private readonly IGamificationService _gamification;
    public LeaderboardsController(IGamificationService gamification) => _gamification = gamification;

    [HttpGet("class/{classId:guid}")]
    public async Task<IActionResult> GetClassLeaderboard(Guid classId, [FromQuery] string period = "all")
        => Ok(await _gamification.GetClassLeaderboardAsync(classId, period));

    [HttpGet("global")]
    public async Task<IActionResult> GetGlobalLeaderboard([FromQuery] string period = "all")
        => Ok(await _gamification.GetGlobalLeaderboardAsync(period));
}
