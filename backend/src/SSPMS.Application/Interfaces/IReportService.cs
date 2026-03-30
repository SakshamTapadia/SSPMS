using SSPMS.Application.DTOs.Reports;

namespace SSPMS.Application.Interfaces;

public interface IReportService
{
    Task<ClassReportDto> GetClassReportAsync(Guid classId, Guid requesterId, string role);
    Task<EmployeeReportDto> GetEmployeeReportAsync(Guid employeeId, Guid requesterId, string role);
    Task<AdminSystemReportDto> GetSystemReportAsync();
    Task<byte[]> ExportClassReportPdfAsync(Guid classId, Guid requesterId, string role);
    Task<byte[]> ExportClassReportExcelAsync(Guid classId, Guid requesterId, string role);
    Task<byte[]> ExportEmployeeReportPdfAsync(Guid employeeId, Guid requesterId, string role);
    Task<byte[]> ExportTaskResultsGridExcelAsync(Guid taskId);
}
