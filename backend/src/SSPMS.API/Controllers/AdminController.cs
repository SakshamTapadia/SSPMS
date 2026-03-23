using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly IReportService _reports;
    private readonly IAuditService _audit;

    public AdminController(IReportService reports, IAuditService audit)
    {
        _reports = reports;
        _audit = audit;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
        => Ok(await _reports.GetSystemReportAsync());

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] Guid? userId = null, [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        => Ok(await _audit.GetLogsAsync(page, pageSize, userId, action, from, to));

    [HttpGet("trainers/report")]
    public async Task<IActionResult> TrainersReport()
    {
        var report = await _reports.GetSystemReportAsync();
        return Ok(report.TrainerSummaries);
    }

    [HttpGet("classes/report")]
    public async Task<IActionResult> ClassesReport()
    {
        var report = await _reports.GetSystemReportAsync();
        return Ok(report.ClassSummaries);
    }
}
