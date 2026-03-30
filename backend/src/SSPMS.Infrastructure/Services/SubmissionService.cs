using Microsoft.EntityFrameworkCore;
using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Submissions;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Entities;
using SSPMS.Domain.Enums;
using SSPMS.Infrastructure.Data;

namespace SSPMS.Infrastructure.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ApplicationDbContext _db;
    private readonly IGamificationService _gamification;
    private readonly INotificationService _notifications;
    private readonly IEmailService _email;
    private readonly ISignalRService _signalR;

    public SubmissionService(ApplicationDbContext db, IGamificationService gamification,
        INotificationService notifications, IEmailService email, ISignalRService signalR)
    {
        _db = db;
        _gamification = gamification;
        _notifications = notifications;
        _email = email;
        _signalR = signalR;
    }

    public async Task<ServiceResult<SubmissionDto>> StartSubmissionAsync(Guid taskId, Guid employeeId)
    {
        var task = await _db.Tasks.Include(t => t.Questions).ThenInclude(q => q.Options).FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return ServiceResult<SubmissionDto>.Failure("Task not found.");
        if (task.Status != Domain.Enums.AssignmentStatus.Published) return ServiceResult<SubmissionDto>.Failure("Task is not available.");
        if (DateTime.UtcNow < task.StartAt) return ServiceResult<SubmissionDto>.Failure("Task has not started yet.");
        if (DateTime.UtcNow > task.EndAt) return ServiceResult<SubmissionDto>.Failure("Task has ended.");

        var existing = await _db.Submissions.FirstOrDefaultAsync(s => s.TaskId == taskId && s.EmployeeId == employeeId);
        if (existing != null)
        {
            if (existing.Status == SubmissionStatus.Submitted || existing.Status == SubmissionStatus.Evaluated)
                return ServiceResult<SubmissionDto>.Failure("Task already submitted.");
            return ServiceResult<SubmissionDto>.Success(MapToDto(existing, task));
        }

        var submission = new Submission { TaskId = taskId, EmployeeId = employeeId, StartedAt = DateTime.UtcNow, Status = SubmissionStatus.Draft };

        // Pre-create answer slots
        foreach (var q in task.Questions)
            submission.Answers.Add(new SubmissionAnswer { QuestionId = q.Id });

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();
        return ServiceResult<SubmissionDto>.Success(MapToDto(submission, task));
    }

    public async Task<ServiceResult<SubmissionDto>> GetByIdAsync(Guid id, Guid requesterId, string role)
    {
        var submission = await _db.Submissions
            .Include(s => s.Task).ThenInclude(t => t.Questions)
            .Include(s => s.Answers)
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (submission == null) return ServiceResult<SubmissionDto>.Failure("Not found.");

        if (role == "Employee" && submission.EmployeeId != requesterId)
            return ServiceResult<SubmissionDto>.Failure("Access denied.");

        return ServiceResult<SubmissionDto>.Success(MapToDto(submission, submission.Task));
    }

    public async Task<ServiceResult<SubmissionDto>> SaveDraftAsync(Guid submissionId, SaveDraftRequest request, Guid employeeId)
    {
        var submission = await _db.Submissions.Include(s => s.Answers).Include(s => s.Task).FirstOrDefaultAsync(s => s.Id == submissionId && s.EmployeeId == employeeId);
        if (submission == null) return ServiceResult<SubmissionDto>.Failure("Not found.");
        if (submission.Status != SubmissionStatus.Draft) return ServiceResult<SubmissionDto>.Failure("Already submitted.");

        foreach (var item in request.Answers)
        {
            var answer = submission.Answers.FirstOrDefault(a => a.QuestionId == item.QuestionId);
            if (answer != null) answer.AnswerText = item.AnswerText;
        }

        await _db.SaveChangesAsync();
        return ServiceResult<SubmissionDto>.Success(MapToDto(submission, submission.Task));
    }

    public async Task<ServiceResult<SubmissionDto>> SubmitAsync(Guid submissionId, Guid employeeId)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var submission = await _db.Submissions
                    .Include(s => s.Task).ThenInclude(t => t.Questions).ThenInclude(q => q.Options)
                    .Include(s => s.Answers)
                    .FirstOrDefaultAsync(s => s.Id == submissionId && s.EmployeeId == employeeId);
                if (submission == null) return ServiceResult<SubmissionDto>.Failure("Not found.");
                if (submission.Status != SubmissionStatus.Draft) return ServiceResult<SubmissionDto>.Failure("Already submitted.");
                var personalDeadline = (submission.StartedAt ?? DateTime.UtcNow).AddMinutes(submission.Task.DurationMinutes);
                var effectiveDeadline = submission.Task.EndAt < personalDeadline ? submission.Task.EndAt : personalDeadline;
                if (DateTime.UtcNow > effectiveDeadline) return ServiceResult<SubmissionDto>.Failure("Task deadline passed.");

                // Assign rank atomically
                var rank = await _db.Submissions.CountAsync(s => s.TaskId == submission.TaskId && s.Status != SubmissionStatus.Draft) + 1;
                submission.SubmissionRank = rank;
                submission.Multiplier = _gamification.GetMultiplierForRank(rank);
                submission.SubmittedAt = DateTime.UtcNow;
                submission.Status = SubmissionStatus.Submitted;

                // Auto-grade MCQs
                decimal rawTotal = 0;
                foreach (var answer in submission.Answers)
                {
                    var question = submission.Task.Questions.First(q => q.Id == answer.QuestionId);
                    if (question.Type == QuestionType.MCQ)
                    {
                        var correct = question.Options.FirstOrDefault(o => o.IsCorrect);
                        var isCorrect = correct != null && answer.AnswerText == correct.Id.ToString();
                        answer.RawScore = isCorrect ? question.Marks : 0;
                        answer.FinalScore = answer.RawScore * submission.Multiplier;
                        rawTotal += answer.RawScore.Value;
                    }
                }
                submission.TotalRawScore = rawTotal;
                submission.TotalFinalScore = rawTotal * submission.Multiplier;

                // Auto-evaluate if every question is MCQ (no manual grading needed)
                bool allMcq = submission.Task.Questions.Any() &&
                              submission.Task.Questions.All(q => q.Type == QuestionType.MCQ);
                if (allMcq)
                    submission.Status = SubmissionStatus.Evaluated;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Update live submission counter
                var count = await _db.Submissions.CountAsync(s => s.TaskId == submission.TaskId && s.Status != SubmissionStatus.Draft);
                await _signalR.SendSubmissionCountUpdateAsync(submission.TaskId, count);

                // Award XP and badges
                await _gamification.ProcessBadgesForSubmissionAsync(submission.Id);

                // Notify employee immediately if auto-evaluated
                if (allMcq)
                    await _notifications.SendNotificationAsync(submission.EmployeeId,
                        $"Results ready: {submission.Task.Title}",
                        $"Your MCQ submission has been auto-graded. Score: {submission.TotalFinalScore:0.#} / {submission.Task.TotalMarks}",
                        NotificationType.TaskEvaluated);

                return ServiceResult<SubmissionDto>.Success(MapToDto(submission, submission.Task));
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<ServiceResult<SubmissionDto>> MalpracticeSubmitAsync(Guid submissionId, Guid employeeId, int tabSwitchCount)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
        using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var submission = await _db.Submissions
                .Include(s => s.Task).ThenInclude(t => t.Questions).ThenInclude(q => q.Options)
                .Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.EmployeeId == employeeId);
            if (submission == null) return ServiceResult<SubmissionDto>.Failure("Not found.");
            if (submission.Status != SubmissionStatus.Draft) return ServiceResult<SubmissionDto>.Failure("Already submitted.");

            var rank = await _db.Submissions.CountAsync(s => s.TaskId == submission.TaskId && s.Status != SubmissionStatus.Draft) + 1;
            submission.SubmissionRank = rank;
            submission.Multiplier = _gamification.GetMultiplierForRank(rank);
            submission.SubmittedAt = DateTime.UtcNow;
            submission.Status = SubmissionStatus.Submitted;
            submission.IsAutoSubmitted = true;
            submission.IsMalpractice = true;
            submission.TabSwitchCount = tabSwitchCount;

            // Auto-grade MCQs for record, but final score is zeroed for malpractice
            decimal rawTotal = 0;
            foreach (var answer in submission.Answers)
            {
                var question = submission.Task.Questions.First(q => q.Id == answer.QuestionId);
                if (question.Type == QuestionType.MCQ)
                {
                    var correct = question.Options.FirstOrDefault(o => o.IsCorrect);
                    var isCorrect = correct != null && answer.AnswerText == correct.Id.ToString();
                    answer.RawScore = isCorrect ? question.Marks : 0;
                    rawTotal += answer.RawScore.Value;
                }
                answer.FinalScore = 0; // malpractice — no credit
            }
            submission.TotalRawScore = rawTotal;
            submission.TotalFinalScore = 0;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var count = await _db.Submissions.CountAsync(s => s.TaskId == submission.TaskId && s.Status != SubmissionStatus.Draft);
            await _signalR.SendSubmissionCountUpdateAsync(submission.TaskId, count);

            // Notify trainer
            await _notifications.SendNotificationAsync(submission.Task.CreatedByTrainerId,
                $"Malpractice detected: {submission.Task.Title}",
                $"A submission was auto-submitted after {tabSwitchCount} screen switch(es).",
                NotificationType.TaskEvaluated);

            return ServiceResult<SubmissionDto>.Success(MapToDto(submission, submission.Task));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
        });
    }

    public async Task<IEnumerable<SubmissionSummary>> GetTaskSubmissionsAsync(Guid taskId, Guid trainerId)
    {
        return await _db.Submissions
            .Include(s => s.Employee)
            .Where(s => s.TaskId == taskId)
            .OrderBy(s => s.SubmissionRank ?? int.MaxValue)
            .Select(s => new SubmissionSummary(s.Id, s.EmployeeId, s.Employee.Name, s.SubmittedAt, s.SubmissionRank, s.Multiplier, s.TotalRawScore, s.TotalFinalScore, s.Status))
            .ToListAsync();
    }

    public async Task<ServiceResult<SubmissionDto>> GetMySubmissionAsync(Guid taskId, Guid employeeId)
    {
        var submission = await _db.Submissions
            .Include(s => s.Task)
            .Include(s => s.Answers)
            .FirstOrDefaultAsync(s => s.TaskId == taskId && s.EmployeeId == employeeId);
        if (submission == null) return ServiceResult<SubmissionDto>.Failure("No submission found.");
        return ServiceResult<SubmissionDto>.Success(MapToDto(submission, submission.Task));
    }

    public async Task<ServiceResult<SubmissionDto>> EvaluateAsync(Guid submissionId, EvaluateSubmissionRequest request, Guid trainerId)
    {
        var submission = await _db.Submissions
            .Include(s => s.Task).ThenInclude(t => t.Questions)
            .Include(s => s.Answers)
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission == null) return ServiceResult<SubmissionDto>.Failure("Not found.");

        decimal rawTotal = 0;
        foreach (var eval in request.Answers)
        {
            var answer = submission.Answers.FirstOrDefault(a => a.Id == eval.AnswerId);
            if (answer == null) continue;
            answer.RawScore = eval.RawScore;
            answer.FinalScore = eval.IsPlagiarismFlag ? 0 : eval.RawScore * (submission.Multiplier ?? 1);
            answer.EvaluatorNote = eval.EvaluatorNote;
            answer.IsPlagiarismFlag = eval.IsPlagiarismFlag;
            rawTotal += answer.RawScore.Value;
        }

        submission.TotalRawScore = rawTotal;
        submission.TotalFinalScore = rawTotal * (submission.Multiplier ?? 1);
        submission.Status = SubmissionStatus.Evaluated;
        await _db.SaveChangesAsync();

        // Notify employee
        await _notifications.SendNotificationAsync(submission.EmployeeId, $"Results ready: {submission.Task.Title}",
            $"Your submission has been evaluated. Final score: {submission.TotalFinalScore}", NotificationType.TaskEvaluated);
        try { await _email.SendTaskEvaluatedEmailAsync(submission.Employee.Email, submission.Employee.Name, submission.Task.Title, submission.TotalFinalScore ?? 0); } catch { }

        // Process badges after evaluation (non-critical — never fail the evaluation if this throws)
        try { await _gamification.ProcessBadgesForSubmissionAsync(submission.Id); } catch { }

        return ServiceResult<SubmissionDto>.Success(MapToDto(submission, submission.Task));
    }

    public async Task<ServiceResult> SetPlagiarismFlagAsync(Guid answerId, bool flag, Guid trainerId)
    {
        var answer = await _db.SubmissionAnswers.Include(a => a.Submission).FirstOrDefaultAsync(a => a.Id == answerId);
        if (answer == null) return ServiceResult.Failure("Not found.");
        answer.IsPlagiarismFlag = flag;
        if (flag) { answer.FinalScore = 0; }
        else { answer.FinalScore = answer.RawScore * answer.Submission.Multiplier; }
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> BulkCompleteEvaluationAsync(Guid taskId, Guid trainerId)
    {
        var subs = await _db.Submissions
            .Include(s => s.Employee)
            .Include(s => s.Task)
            .Where(s => s.TaskId == taskId && s.Status == SubmissionStatus.Submitted)
            .ToListAsync();

        foreach (var s in subs)
        {
            s.Status = SubmissionStatus.Evaluated;
            await _notifications.SendNotificationAsync(s.EmployeeId, $"Results ready: {s.Task.Title}", "Your results are ready.", NotificationType.TaskEvaluated);
        }
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task ProcessExpiredSubmissionsAsync()
    {
        var now = DateTime.UtcNow;

        // Auto-submit drafts where the student's personal duration has elapsed mid-window
        var personallyExpired = await _db.Submissions
            .Include(s => s.Task)
            .Where(s => s.Status == SubmissionStatus.Draft
                     && s.SubmittedAt == null
                     && s.Task.Status == Domain.Enums.AssignmentStatus.Published)
            .ToListAsync();

        foreach (var sub in personallyExpired)
        {
            var personalDeadline = (sub.StartedAt ?? now).AddMinutes(sub.Task.DurationMinutes);
            if (now >= personalDeadline && now < sub.Task.EndAt)
            {
                sub.SubmittedAt = now;
                sub.SubmissionRank = null;
                sub.Multiplier = 0;
                sub.TotalRawScore = 0;
                sub.TotalFinalScore = 0;
                sub.Status = SubmissionStatus.Submitted;
                sub.IsAutoSubmitted = true;
            }
        }

        var expiredTasks = await _db.Tasks
            .Where(t => t.Status == Domain.Enums.AssignmentStatus.Published && t.EndAt < now)
            .ToListAsync();

        foreach (var task in expiredTasks)
        {
            task.Status = Domain.Enums.AssignmentStatus.Closed;

            var draftSubmissions = await _db.Submissions
                .Where(s => s.TaskId == task.Id && s.Status == SubmissionStatus.Draft && s.SubmittedAt == null)
                .ToListAsync();

            foreach (var sub in draftSubmissions)
            {
                sub.SubmittedAt = now;
                sub.SubmissionRank = null;
                sub.Multiplier = 0;
                sub.TotalRawScore = 0;
                sub.TotalFinalScore = 0;
                sub.Status = SubmissionStatus.Submitted;
                sub.IsAutoSubmitted = true;
            }

            // Create zero-score submissions for enrolled employees who never started
            var submittedEmployeeIds = await _db.Submissions.Where(s => s.TaskId == task.Id).Select(s => s.EmployeeId).ToListAsync();
            var enrolledEmployeeIds = await _db.ClassEnrollments.Where(e => e.ClassId == task.ClassId && e.Status == EnrollmentStatus.Active).Select(e => e.EmployeeId).ToListAsync();
            var missingIds = enrolledEmployeeIds.Except(submittedEmployeeIds);

            foreach (var empId in missingIds)
            {
                _db.Submissions.Add(new Submission
                {
                    TaskId = task.Id,
                    EmployeeId = empId,
                    SubmittedAt = now,
                    SubmissionRank = null,
                    Multiplier = 0,
                    TotalRawScore = 0,
                    TotalFinalScore = 0,
                    Status = SubmissionStatus.Submitted,
                    IsAutoSubmitted = true
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    private static SubmissionDto MapToDto(Submission s, AssignedTask? task) => new(
        s.Id, s.TaskId, task?.Title ?? "", task?.EndAt ?? DateTime.MinValue, task?.DurationMinutes ?? 0,
        s.EmployeeId, s.Employee?.Name ?? "",
        s.StartedAt, s.SubmittedAt, s.SubmissionRank, s.Multiplier,
        s.TotalRawScore, s.TotalFinalScore, s.Status, s.IsAutoSubmitted,
        s.IsMalpractice, s.TabSwitchCount,
        s.Answers.Select(a => new SubmissionAnswerDto(a.Id, a.QuestionId, a.AnswerText, a.RawScore, a.FinalScore, a.EvaluatorNote, a.IsPlagiarismFlag))
    );
}
