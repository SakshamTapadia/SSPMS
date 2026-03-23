using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Authorize]
[Route("api/v1/analytics")]
public class AnalyticsController : BaseController
{
    private readonly IAnalyticsService _analytics;
    public AnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    [HttpGet("task/{taskId:guid}/blind-spots")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetBlindSpots(Guid taskId)
    {
        var result = await _analytics.GetTaskBlindSpotsAsync(taskId);
        return Ok(result);
    }

    [HttpGet("task/{taskId:guid}/code-similarity")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetCodeSimilarity(Guid taskId)
    {
        var result = await _analytics.GetCodeSimilarityAsync(taskId);
        return Ok(result);
    }

    [HttpGet("employee/{employeeId:guid}/velocity")]
    public async Task<IActionResult> GetEmployeeVelocity(Guid employeeId)
    {
        var result = await _analytics.GetEmployeeVelocityAsync(employeeId);
        return Ok(result);
    }

    [HttpGet("class/{classId:guid}/velocity")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetClassVelocity(Guid classId)
    {
        var result = await _analytics.GetClassVelocityAsync(classId);
        return Ok(result);
    }
}
