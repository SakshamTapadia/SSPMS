using SSPMS.Application.DTOs.Analytics;

namespace SSPMS.Application.Interfaces;

public interface IAnalyticsService
{
    Task<TaskBlindSpotReport> GetTaskBlindSpotsAsync(Guid taskId);
    Task<CodeSimilarityReport> GetCodeSimilarityAsync(Guid taskId);
    Task<EmployeeVelocityDto> GetEmployeeVelocityAsync(Guid employeeId);
    Task<ClassVelocityReport> GetClassVelocityAsync(Guid classId);
}
