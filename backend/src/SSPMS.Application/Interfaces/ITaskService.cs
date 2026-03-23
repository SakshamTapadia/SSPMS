using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Tasks;

namespace SSPMS.Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskDto>> GetTasksAsync(Guid trainerId, Guid? classId = null);
    Task<IEnumerable<TaskDto>> GetMyTasksAsync(Guid employeeId);
    Task<ServiceResult<TaskDto>> GetByIdAsync(Guid id, Guid requesterId, string role);
    Task<ServiceResult<TaskDto>> CreateTaskAsync(CreateTaskRequest request, Guid trainerId);
    Task<ServiceResult<TaskDto>> UpdateTaskAsync(Guid id, UpdateTaskRequest request, Guid trainerId);
    Task<ServiceResult> DeleteTaskAsync(Guid id, Guid trainerId);
    Task<ServiceResult> PublishTaskAsync(Guid id, Guid trainerId);
    Task<ServiceResult<TaskDto>> DuplicateTaskAsync(Guid id, Guid trainerId);
    Task<IEnumerable<QuestionDto>> GetQuestionsAsync(Guid taskId, bool includeAnswers);
    Task<ServiceResult<QuestionDto>> AddQuestionAsync(Guid taskId, CreateQuestionRequest request, Guid trainerId);
    Task<ServiceResult<QuestionDto>> UpdateQuestionAsync(Guid taskId, Guid questionId, CreateQuestionRequest request, Guid trainerId);
    Task<ServiceResult> DeleteQuestionAsync(Guid taskId, Guid questionId, Guid trainerId);
    Task<ServiceResult> ReorderQuestionsAsync(Guid taskId, IEnumerable<Guid> questionIds, Guid trainerId);
}
