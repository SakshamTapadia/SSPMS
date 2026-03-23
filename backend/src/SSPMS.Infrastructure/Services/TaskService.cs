using Microsoft.EntityFrameworkCore;
using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Tasks;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Entities;
using SSPMS.Domain.Enums;
using SSPMS.Infrastructure.Data;

namespace SSPMS.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IEmailService _email;

    public TaskService(ApplicationDbContext db, INotificationService notifications, IEmailService email)
    {
        _db = db;
        _notifications = notifications;
        _email = email;
    }

    public async Task<IEnumerable<TaskDto>> GetTasksAsync(Guid trainerId, Guid? classId)
    {
        var trainerClassIds = await _db.Classes.Where(c => c.TrainerId == trainerId).Select(c => c.Id).ToListAsync();
        var query = _db.Tasks.Include(t => t.Class).Include(t => t.CreatedByTrainer)
            .Where(t => trainerClassIds.Contains(t.ClassId));
        if (classId.HasValue) query = query.Where(t => t.ClassId == classId);
        return await query.OrderByDescending(t => t.CreatedAt).Select(t => MapDto(t)).ToListAsync();
    }

    public async Task<IEnumerable<TaskDto>> GetMyTasksAsync(Guid employeeId)
    {
        var enrollment = await _db.ClassEnrollments.FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.Status == EnrollmentStatus.Active);
        if (enrollment == null) return [];
        return await _db.Tasks.Include(t => t.Class).Include(t => t.CreatedByTrainer)
            .Where(t => t.ClassId == enrollment.ClassId && t.Status == Domain.Enums.AssignmentStatus.Published)
            .OrderByDescending(t => t.StartAt)
            .Select(t => MapDto(t)).ToListAsync();
    }

    public async Task<ServiceResult<TaskDto>> GetByIdAsync(Guid id, Guid requesterId, string role)
    {
        var task = await _db.Tasks.Include(t => t.Class).Include(t => t.CreatedByTrainer).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return ServiceResult<TaskDto>.Failure("Task not found.");
        return ServiceResult<TaskDto>.Success(MapDto(task));
    }

    public async Task<ServiceResult<TaskDto>> CreateTaskAsync(CreateTaskRequest request, Guid trainerId)
    {
        var @class = await _db.Classes.FindAsync(request.ClassId);
        if (@class == null || @class.TrainerId != trainerId)
            return ServiceResult<TaskDto>.Failure("Class not found or access denied.");

        var task = new AssignedTask
        {
            ClassId = request.ClassId,
            Title = request.Title,
            Description = request.Description,
            Instructions = request.Instructions,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            DurationMinutes = request.DurationMinutes,
            Status = Domain.Enums.AssignmentStatus.Draft,
            CreatedByTrainerId = trainerId
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        await _db.Entry(task).Reference(t => t.Class).LoadAsync();
        await _db.Entry(task).Reference(t => t.CreatedByTrainer).LoadAsync();
        return ServiceResult<TaskDto>.Success(MapDto(task));
    }

    public async Task<ServiceResult<TaskDto>> UpdateTaskAsync(Guid id, UpdateTaskRequest request, Guid trainerId)
    {
        var task = await _db.Tasks.Include(t => t.Class).Include(t => t.CreatedByTrainer).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null || task.CreatedByTrainerId != trainerId) return ServiceResult<TaskDto>.Failure("Not found or access denied.");
        if (task.Status != Domain.Enums.AssignmentStatus.Draft) return ServiceResult<TaskDto>.Failure("Cannot edit a published task.");

        task.Title = request.Title;
        task.Description = request.Description;
        task.Instructions = request.Instructions;
        task.StartAt = request.StartAt;
        task.EndAt = request.EndAt;
        task.DurationMinutes = request.DurationMinutes;
        await _db.SaveChangesAsync();
        return ServiceResult<TaskDto>.Success(MapDto(task));
    }

    public async Task<ServiceResult> DeleteTaskAsync(Guid id, Guid trainerId)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null || task.CreatedByTrainerId != trainerId) return ServiceResult.Failure("Not found.");
        if (task.Status != Domain.Enums.AssignmentStatus.Draft) return ServiceResult.Failure("Cannot delete a published task.");
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> PublishTaskAsync(Guid id, Guid trainerId)
    {
        var task = await _db.Tasks.Include(t => t.Questions).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null || task.CreatedByTrainerId != trainerId) return ServiceResult.Failure("Not found.");
        if (task.Status != Domain.Enums.AssignmentStatus.Draft) return ServiceResult.Failure("Task already published.");
        if (!task.Questions.Any()) return ServiceResult.Failure("Task must have at least one question.");

        task.TotalMarks = task.Questions.Sum(q => q.Marks);
        task.Status = Domain.Enums.AssignmentStatus.Published;
        await _db.SaveChangesAsync();

        // Notify employees
        await _notifications.SendClassNotificationAsync(task.ClassId, $"New Task: {task.Title}",
            $"A new task is available. Opens: {task.StartAt:dd MMM HH:mm} UTC", NotificationType.TaskAssigned);

        // Email employees
        var employees = await _db.ClassEnrollments
            .Include(e => e.Employee)
            .Where(e => e.ClassId == task.ClassId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.Employee)
            .ToListAsync();
        foreach (var emp in employees)
            try { await _email.SendTaskAssignedEmailAsync(emp.Email, emp.Name, task.Title, task.StartAt, task.EndAt); } catch { }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<TaskDto>> DuplicateTaskAsync(Guid id, Guid trainerId)
    {
        var task = await _db.Tasks.Include(t => t.Questions).ThenInclude(q => q.Options).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null || task.CreatedByTrainerId != trainerId) return ServiceResult<TaskDto>.Failure("Not found.");

        var copy = new AssignedTask
        {
            ClassId = task.ClassId,
            Title = $"Copy of {task.Title}",
            Description = task.Description,
            Instructions = task.Instructions,
            TotalMarks = task.TotalMarks,
            StartAt = task.StartAt.AddDays(7),
            EndAt = task.EndAt.AddDays(7),
            DurationMinutes = task.DurationMinutes,
            Status = Domain.Enums.AssignmentStatus.Draft,
            CreatedByTrainerId = trainerId
        };
        foreach (var q in task.Questions)
        {
            var newQ = new Question { TaskId = copy.Id, Type = q.Type, Stem = q.Stem, Marks = q.Marks, OrderIndex = q.OrderIndex, Language = q.Language, ExpectedOutput = q.ExpectedOutput };
            foreach (var o in q.Options)
                newQ.Options.Add(new MCQOption { OptionText = o.OptionText, IsCorrect = o.IsCorrect, OrderIndex = o.OrderIndex });
            copy.Questions.Add(newQ);
        }
        _db.Tasks.Add(copy);
        await _db.SaveChangesAsync();
        return ServiceResult<TaskDto>.Success(MapDto(copy));
    }

    public async Task<IEnumerable<QuestionDto>> GetQuestionsAsync(Guid taskId, bool includeAnswers)
    {
        var questions = await _db.Questions
            .Include(q => q.Options)
            .Where(q => q.TaskId == taskId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        return questions.Select(q => new QuestionDto(
            q.Id, q.TaskId, q.Type, q.Stem, q.Marks, q.OrderIndex, q.Language,
            q.Type == QuestionType.MCQ ? q.Options.OrderBy(o => o.OrderIndex).Select(o => new MCQOptionDto(o.Id, o.OptionText, o.OrderIndex, includeAnswers ? o.IsCorrect : null)) : null
        ));
    }

    public async Task<ServiceResult<QuestionDto>> AddQuestionAsync(Guid taskId, CreateQuestionRequest request, Guid trainerId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task == null || task.CreatedByTrainerId != trainerId) return ServiceResult<QuestionDto>.Failure("Not found.");
        if (task.Status != Domain.Enums.AssignmentStatus.Draft) return ServiceResult<QuestionDto>.Failure("Cannot edit published task.");

        var question = new Question { TaskId = taskId, Type = request.Type, Stem = request.Stem, Marks = request.Marks, OrderIndex = request.OrderIndex, Language = request.Language, ExpectedOutput = request.ExpectedOutput };

        if (request.Type == QuestionType.MCQ && request.Options != null)
            foreach (var o in request.Options)
                question.Options.Add(new MCQOption { OptionText = o.OptionText, IsCorrect = o.IsCorrect, OrderIndex = o.OrderIndex });

        _db.Questions.Add(question);
        await _db.SaveChangesAsync();

        return ServiceResult<QuestionDto>.Success(new QuestionDto(
            question.Id, question.TaskId, question.Type, question.Stem, question.Marks, question.OrderIndex, question.Language,
            question.Options.Select(o => new MCQOptionDto(o.Id, o.OptionText, o.OrderIndex, o.IsCorrect))));
    }

    public async Task<ServiceResult<QuestionDto>> UpdateQuestionAsync(Guid taskId, Guid questionId, CreateQuestionRequest request, Guid trainerId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task == null || task.CreatedByTrainerId != trainerId || task.Status != Domain.Enums.AssignmentStatus.Draft)
            return ServiceResult<QuestionDto>.Failure("Not found or access denied.");

        var question = await _db.Questions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == questionId && q.TaskId == taskId);
        if (question == null) return ServiceResult<QuestionDto>.Failure("Question not found.");

        question.Stem = request.Stem;
        question.Marks = request.Marks;
        question.Language = request.Language;
        question.ExpectedOutput = request.ExpectedOutput;

        if (request.Type == QuestionType.MCQ && request.Options != null)
        {
            _db.MCQOptions.RemoveRange(question.Options);
            foreach (var o in request.Options)
                question.Options.Add(new MCQOption { OptionText = o.OptionText, IsCorrect = o.IsCorrect, OrderIndex = o.OrderIndex });
        }
        await _db.SaveChangesAsync();
        return ServiceResult<QuestionDto>.Success(new QuestionDto(question.Id, question.TaskId, question.Type, question.Stem, question.Marks, question.OrderIndex, question.Language, question.Options.Select(o => new MCQOptionDto(o.Id, o.OptionText, o.OrderIndex, o.IsCorrect))));
    }

    public async Task<ServiceResult> DeleteQuestionAsync(Guid taskId, Guid questionId, Guid trainerId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task == null || task.CreatedByTrainerId != trainerId || task.Status != Domain.Enums.AssignmentStatus.Draft)
            return ServiceResult.Failure("Access denied.");
        var question = await _db.Questions.FindAsync(questionId);
        if (question == null) return ServiceResult.Failure("Not found.");
        _db.Questions.Remove(question);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ReorderQuestionsAsync(Guid taskId, IEnumerable<Guid> questionIds, Guid trainerId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task == null || task.CreatedByTrainerId != trainerId) return ServiceResult.Failure("Access denied.");
        var questions = await _db.Questions.Where(q => q.TaskId == taskId).ToListAsync();
        int idx = 0;
        foreach (var id in questionIds)
        {
            var q = questions.FirstOrDefault(x => x.Id == id);
            if (q != null) q.OrderIndex = idx++;
        }
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    private static TaskDto MapDto(AssignedTask t) => new(
        t.Id, t.ClassId, t.Class?.Name ?? "", t.Title, t.Description, t.Instructions,
        t.TotalMarks, t.StartAt, t.EndAt, t.DurationMinutes, t.Status,
        t.CreatedByTrainerId, t.CreatedByTrainer?.Name ?? "",
        t.CreatedAt, t.Questions.Count, t.Submissions.Count);
}
