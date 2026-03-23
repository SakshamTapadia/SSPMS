using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/reports")]
[Authorize]
public class ReportsController : BaseController
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("class/{classId:guid}"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetClassReport(Guid classId)
        => Ok(await _reports.GetClassReportAsync(classId, CurrentUserId, CurrentUserRole));

    [HttpGet("class/{classId:guid}/export"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> ExportClassReport(Guid classId, [FromQuery] string format = "pdf")
    {
        if (format == "xlsx")
        {
            var bytes = await _reports.ExportClassReportExcelAsync(classId, CurrentUserId, CurrentUserRole);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "class-report.xlsx");
        }
        else
        {
            var bytes = await _reports.ExportClassReportPdfAsync(classId, CurrentUserId, CurrentUserRole);
            return File(bytes, "application/pdf", "class-report.pdf");
        }
    }

    [HttpGet("me"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyReport()
        => Ok(await _reports.GetEmployeeReportAsync(CurrentUserId, CurrentUserId, CurrentUserRole));

    [HttpGet("me/export"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> ExportMyReport()
    {
        var bytes = await _reports.ExportEmployeeReportPdfAsync(CurrentUserId, CurrentUserId, CurrentUserRole);
        return File(bytes, "application/pdf", "my-report.pdf");
    }

    [HttpGet("admin/system"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSystemReport()
        => Ok(await _reports.GetSystemReportAsync());
}
