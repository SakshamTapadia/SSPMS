using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Submissions;

namespace SSPMS.Application.Interfaces;

public interface ISubmissionService
{
    Task<ServiceResult<SubmissionDto>> StartSubmissionAsync(Guid taskId, Guid employeeId);
    Task<ServiceResult<SubmissionDto>> GetByIdAsync(Guid id, Guid requesterId, string role);
    Task<ServiceResult<SubmissionDto>> SaveDraftAsync(Guid submissionId, SaveDraftRequest request, Guid employeeId);
    Task<ServiceResult<SubmissionDto>> SubmitAsync(Guid submissionId, Guid employeeId);
    Task<ServiceResult<SubmissionDto>> MalpracticeSubmitAsync(Guid submissionId, Guid employeeId, int tabSwitchCount);
    Task<IEnumerable<SubmissionSummary>> GetTaskSubmissionsAsync(Guid taskId, Guid trainerId);
    Task<ServiceResult<SubmissionDto>> GetMySubmissionAsync(Guid taskId, Guid employeeId);
    Task<ServiceResult<SubmissionDto>> EvaluateAsync(Guid submissionId, EvaluateSubmissionRequest request, Guid trainerId);
    Task<ServiceResult> SetPlagiarismFlagAsync(Guid answerId, bool flag, Guid trainerId);
    Task<ServiceResult> BulkCompleteEvaluationAsync(Guid taskId, Guid trainerId);
    Task ProcessExpiredSubmissionsAsync();
}
