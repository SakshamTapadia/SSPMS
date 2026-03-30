using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Tasks;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Entities;
using SSPMS.Domain.Enums;
using SSPMS.Infrastructure.Data;
using UglyToad.PdfPig;

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

    public async Task<IEnumerable<TaskDto>> GetTasksAsync(Guid? trainerId, Guid? classId)
    {
        var query = _db.Tasks.Include(t => t.Class).Include(t => t.CreatedByTrainer).AsQueryable();
        if (trainerId.HasValue)
        {
            var trainerClassIds = await _db.Classes.Where(c => c.TrainerId == trainerId).Select(c => c.Id).ToListAsync();
            query = query.Where(t => trainerClassIds.Contains(t.ClassId));
        }
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
        if (request.EndAt <= request.StartAt)
            return ServiceResult<TaskDto>.Failure("End date/time must be after start date/time.");

        var @class = await _db.Classes.FindAsync(request.ClassId);
        if (@class == null) return ServiceResult<TaskDto>.Failure("Class not found.");

        var isAdmin = await IsAdminAsync(trainerId);
        if (!isAdmin && @class.TrainerId != trainerId)
            return ServiceResult<TaskDto>.Failure("Access denied.");

        var effectiveTrainerId = isAdmin ? @class.TrainerId : trainerId;
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
            CreatedByTrainerId = effectiveTrainerId
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        await _db.Entry(task).Reference(t => t.Class).LoadAsync();
        await _db.Entry(task).Reference(t => t.CreatedByTrainer).LoadAsync();
        return ServiceResult<TaskDto>.Success(MapDto(task));
    }

    public async Task<ServiceResult<TaskDto>> UpdateTaskAsync(Guid id, UpdateTaskRequest request, Guid trainerId)
    {
        if (request.EndAt <= request.StartAt)
            return ServiceResult<TaskDto>.Failure("End date/time must be after start date/time.");

        var task = await _db.Tasks.Include(t => t.Class).Include(t => t.CreatedByTrainer).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return ServiceResult<TaskDto>.Failure("Not found.");
        if (task.CreatedByTrainerId != trainerId && !await IsAdminAsync(trainerId)) return ServiceResult<TaskDto>.Failure("Access denied.");
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
        if (task == null) return ServiceResult.Failure("Not found.");
        if (task.CreatedByTrainerId != trainerId && !await IsAdminAsync(trainerId)) return ServiceResult.Failure("Access denied.");
        if (task.Status != Domain.Enums.AssignmentStatus.Draft) return ServiceResult.Failure("Cannot delete a published task.");
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> PublishTaskAsync(Guid id, Guid trainerId)
    {
        var task = await _db.Tasks.Include(t => t.Questions).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return ServiceResult.Failure("Not found.");
        if (task.CreatedByTrainerId != trainerId && !await IsAdminAsync(trainerId)) return ServiceResult.Failure("Access denied.");
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
        if (task == null) return ServiceResult<TaskDto>.Failure("Not found.");
        if (task.CreatedByTrainerId != trainerId && !await IsAdminAsync(trainerId)) return ServiceResult<TaskDto>.Failure("Access denied.");

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
        if (task == null) return ServiceResult<QuestionDto>.Failure("Not found.");
        if (task.CreatedByTrainerId != trainerId && !await IsAdminAsync(trainerId)) return ServiceResult<QuestionDto>.Failure("Access denied.");
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
        if (task == null) return ServiceResult<QuestionDto>.Failure("Not found.");
        if ((task.CreatedByTrainerId != trainerId && !await IsAdminAsync(trainerId)) || task.Status != Domain.Enums.AssignmentStatus.Draft)
            return ServiceResult<QuestionDto>.Failure("Access denied or task is not a draft.");

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

    public async Task<ServiceResult<IEnumerable<QuestionDto>>> ImportQuestionsFromDocumentAsync(
        Guid taskId, Stream fileStream, string fileName, Guid requesterId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task == null) return ServiceResult<IEnumerable<QuestionDto>>.Failure("Task not found.");
        if (task.CreatedByTrainerId != requesterId && !await IsAdminAsync(requesterId))
            return ServiceResult<IEnumerable<QuestionDto>>.Failure("Access denied.");
        if (task.Status != Domain.Enums.AssignmentStatus.Draft)
            return ServiceResult<IEnumerable<QuestionDto>>.Failure("Cannot edit a published task.");

        string text;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        try
        {
            text = ext == ".pdf" ? ExtractPdfText(fileStream) : ExtractDocxText(fileStream);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<QuestionDto>>.Failure($"Failed to read document: {ex.Message}");
        }

        var parsed = ParseMcqQuestions(text);
        if (parsed.Count == 0)
            return ServiceResult<IEnumerable<QuestionDto>>.Failure(
                "No MCQ questions found. Ensure the document uses the format: question line, A) B) C) D) options, Answer: X");

        var existing = await _db.Questions.Where(q => q.TaskId == taskId).CountAsync();
        var added = new List<QuestionDto>();
        int order = existing;

        foreach (var (stem, options, correctLetter) in parsed)
        {
            var q = new Question
            {
                TaskId = taskId,
                Type = QuestionType.MCQ,
                Stem = stem,
                Marks = 10,
                OrderIndex = order++
            };
            char correct = char.ToUpperInvariant(correctLetter);
            for (int i = 0; i < options.Count; i++)
            {
                char letter = (char)('A' + i);
                q.Options.Add(new MCQOption { OptionText = options[i], IsCorrect = letter == correct, OrderIndex = i });
            }
            _db.Questions.Add(q);
            added.Add(new QuestionDto(q.Id, q.TaskId, q.Type, q.Stem, q.Marks, q.OrderIndex, null,
                q.Options.Select(o => new MCQOptionDto(o.Id, o.OptionText, o.OrderIndex, o.IsCorrect))));
        }

        await _db.SaveChangesAsync();
        return ServiceResult<IEnumerable<QuestionDto>>.Success(added);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static string ExtractPdfText(Stream stream)
    {
        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(stream);
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractDocxText(Stream stream)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;
        foreach (var para in body.Descendants<Paragraph>())
            sb.AppendLine(para.InnerText);
        return sb.ToString();
    }

    // Parses blocks like:
    //   1. Question text
    //   A) Option A   B) Option B   C) Option C   D) Option D
    //   Answer: B
    private static List<(string Stem, List<string> Options, char Correct)> ParseMcqQuestions(string text)
    {
        var results = new List<(string, List<string>, char)>();
        // Normalise line endings
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

        var qLine = new Regex(@"^(\d+[\.\)])\s+(.+)$");
        var optLine = new Regex(@"[A-Da-d][\.\)]\s*([^A-Da-d\.\)]+?)(?=\s+[A-Da-d][\.\)]|$)", RegexOptions.IgnoreCase);
        var ansLine = new Regex(@"^[Aa]nswer\s*[:\-]\s*([A-Da-d])", RegexOptions.IgnoreCase);
        var singleOpt = new Regex(@"^([A-Da-d])[\.\)]\s+(.+)$", RegexOptions.IgnoreCase);

        string? stem = null;
        List<string>? opts = null;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // Question line
            var qm = qLine.Match(line);
            if (qm.Success)
            {
                stem = qm.Groups[2].Value.Trim();
                opts = null;
                continue;
            }

            if (stem == null) continue;

            // Options may be on one line (A) ... B) ... C) ... D) ...) or separate lines
            if (opts == null)
            {
                // Try separate option line
                var sm = singleOpt.Match(line);
                if (sm.Success)
                {
                    opts = new List<string> { sm.Groups[2].Value.Trim() };
                    continue;
                }
                // Try all-on-one-line
                var allOpts = optLine.Matches(line);
                if (allOpts.Count >= 2)
                {
                    opts = allOpts.Select(m => m.Groups[1].Value.Trim()).ToList();
                    continue;
                }
            }
            else if (opts.Count < 4)
            {
                var sm = singleOpt.Match(line);
                if (sm.Success) { opts.Add(sm.Groups[2].Value.Trim()); continue; }
            }

            // Answer line
            var am = ansLine.Match(line);
            if (am.Success && stem != null && opts != null && opts.Count >= 2)
            {
                results.Add((stem, opts, am.Groups[1].Value[0]));
                stem = null; opts = null;
            }
        }

        return results;
    }

    private async Task<bool> IsAdminAsync(Guid userId) =>
        await _db.Users.AnyAsync(u => u.Id == userId && u.Role == Domain.Enums.UserRole.Admin);

    private static TaskDto MapDto(AssignedTask t) => new(
        t.Id, t.ClassId, t.Class?.Name ?? "", t.Title, t.Description, t.Instructions,
        t.TotalMarks, t.StartAt, t.EndAt, t.DurationMinutes, t.Status,
        t.CreatedByTrainerId, t.CreatedByTrainer?.Name ?? "",
        t.CreatedAt, t.Questions.Count, t.Submissions.Count);
}
