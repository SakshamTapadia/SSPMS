using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SSPMS.Application.DTOs.Reports;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Enums;
using SSPMS.Infrastructure.Data;

namespace SSPMS.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ClassReportDto> GetClassReportAsync(Guid classId, Guid requesterId, string role)
    {
        var @class = await _db.Classes.Include(c => c.Trainer).FirstOrDefaultAsync(c => c.Id == classId)
            ?? throw new Exception("Class not found.");

        var enrolledIds = await _db.ClassEnrollments
            .Include(e => e.Employee)
            .Where(e => e.ClassId == classId && e.Status == EnrollmentStatus.Active && e.Employee.Role == UserRole.Employee)
            .Select(e => e.EmployeeId)
            .ToListAsync();
        var tasks = await _db.Tasks.Where(t => t.ClassId == classId).ToListAsync();
        var taskIds = tasks.Select(t => t.Id).ToList();
        var submissions = await _db.Submissions.Where(s => taskIds.Contains(s.TaskId)).ToListAsync();

        var taskReports = tasks.Select(t =>
        {
            var taskSubs = submissions.Where(s => s.TaskId == t.Id && s.Status != SubmissionStatus.Draft).ToList();
            var notSub = enrolledIds.Count - taskSubs.Count;
            var scores = taskSubs.Select(s => (double)(s.TotalFinalScore ?? 0)).ToList();
            var rawScores = taskSubs.Select(s => (double)(s.TotalRawScore ?? 0)).ToList();

            var buckets = Enumerable.Range(0, 10).Select(i => new ScoreBucket($"{i * 10}-{(i + 1) * 10}", scores.Count(s => s >= i * 10 && s < (i + 1) * 10))).ToList();

            return new TaskReportItem(t.Id, t.Title, t.StartAt, t.EndAt, t.TotalMarks,
                taskSubs.Count, notSub,
                rawScores.Any() ? (decimal)rawScores.Average() : 0,
                scores.Any() ? (decimal)scores.Average() : 0,
                buckets);
        }).ToList();

        var daily = submissions.Where(s => s.SubmittedAt.HasValue).GroupBy(s => DateOnly.FromDateTime(s.SubmittedAt!.Value))
            .Select(g => new DailyActivityItem(g.Key, g.Count(), g.Any() ? (decimal)g.Average(s => (double)(s.TotalFinalScore ?? 0)) : 0))
            .OrderBy(d => d.Date).ToList();

        var totalSubs = submissions.Count(s => s.Status != SubmissionStatus.Draft);
        var totalPossible = tasks.Count * enrolledIds.Count;
        var avgScore = submissions.Any(s => s.TotalFinalScore.HasValue) ? (decimal)submissions.Where(s => s.TotalFinalScore.HasValue).Average(s => (double)s.TotalFinalScore!.Value) : 0;

        return new ClassReportDto(classId, @class.Name, @class.Trainer.Name, enrolledIds.Count, tasks.Count, Math.Round(avgScore, 1),
            totalPossible > 0 ? Math.Round((decimal)totalSubs / totalPossible * 100, 1) : 0, taskReports, daily);
    }

    public async Task<EmployeeReportDto> GetEmployeeReportAsync(Guid employeeId, Guid requesterId, string role)
    {
        var user = await _db.Users.FindAsync(employeeId) ?? throw new Exception("User not found.");
        var enrollment = await _db.ClassEnrollments.Include(e => e.Class).FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.Status == EnrollmentStatus.Active);
        var submissions = await _db.Submissions.Include(s => s.Task).Where(s => s.EmployeeId == employeeId && s.Status != SubmissionStatus.Draft).ToListAsync();
        var totalXP = await _db.XPLedger.Where(x => x.EmployeeId == employeeId).SumAsync(x => x.Points);

        var taskResults = submissions.Select(s =>
        {
            var classAvg = _db.Submissions.Where(x => x.TaskId == s.TaskId && x.Status != SubmissionStatus.Draft).Average(x => (double)(x.TotalFinalScore ?? 0));
            var classTop = _db.Submissions.Where(x => x.TaskId == s.TaskId && x.Status != SubmissionStatus.Draft).Max(x => (double)(x.TotalFinalScore ?? 0));
            return new EmployeeTaskResult(s.TaskId, s.Task.Title, s.SubmittedAt, s.SubmissionRank, s.TotalRawScore, s.TotalFinalScore, s.Multiplier, (decimal)classAvg, (decimal)classTop, s.Status.ToString());
        }).ToList();

        return new EmployeeReportDto(employeeId, user.Name, enrollment?.Class?.Name ?? "No Class", totalXP, 0, 0,
            submissions.Any() ? (decimal)submissions.Average(s => (double)(s.TotalFinalScore ?? 0)) : 0,
            taskResults, [], []);
    }

    public async Task<AdminSystemReportDto> GetSystemReportAsync()
    {
        var users = await _db.Users.ToListAsync();
        var classes = await _db.Classes.Include(c => c.Trainer).ToListAsync();
        var tasks = await _db.Tasks.ToListAsync();
        var subs = await _db.Submissions.ToListAsync();

        var trainerSummaries = classes.GroupBy(c => c.TrainerId).Select(g => new TrainerReportItem(
            g.Key, g.First().Trainer.Name, g.Count(),
            tasks.Count(t => g.Select(c => c.Id).Contains(t.ClassId)),
            0)).ToList();

        var classSummaries = classes.Select(c =>
        {
            var enrolled = _db.ClassEnrollments.Include(e => e.Employee).Count(e => e.ClassId == c.Id && e.Status == EnrollmentStatus.Active && e.Employee.Role == UserRole.Employee);
            var taskCount = tasks.Count(t => t.ClassId == c.Id);
            var classTaskIds = tasks.Where(t => t.ClassId == c.Id).Select(t => t.Id).ToHashSet();
            var classSubs = subs.Where(s => classTaskIds.Contains(s.TaskId)).ToList();
            var avg = classSubs.Any() ? (decimal)classSubs.Average(s => (double)(s.TotalFinalScore ?? 0)) : 0;
            return new ClassSummaryItem(c.Id, c.Name, c.Trainer.Name, enrolled, taskCount, Math.Round(avg, 1), 0);
        }).ToList();

        return new AdminSystemReportDto(users.Count, users.Count(u => u.Role == UserRole.Trainer),
            users.Count(u => u.Role == UserRole.Employee), classes.Count, tasks.Count, subs.Count,
            trainerSummaries, classSummaries);
    }

    public async Task<byte[]> ExportClassReportPdfAsync(Guid classId, Guid requesterId, string role)
    {
        var report = await GetClassReportAsync(classId, requesterId, role);
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Text($"Class Report: {report.ClassName}").SemiBold().FontSize(18);
                page.Content().Column(col =>
                {
                    col.Item().Text($"Trainer: {report.TrainerName}");
                    col.Item().Text($"Employees: {report.TotalEmployees} | Tasks: {report.TotalTasks} | Avg Score: {report.AvgScore}% | Completion: {report.CompletionRate}%");
                    col.Item().PaddingTop(10).Text("Task Breakdown").SemiBold().FontSize(14);
                    foreach (var t in report.TaskReports)
                    {
                        col.Item().PaddingTop(6).Text($"{t.TaskTitle}: Submissions: {t.SubmittedCount}, Avg Score: {t.AvgFinalScore:F1}");
                    }
                });
                page.Footer().Text(x => { x.Span("Generated by SSPMS | "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages(); });
            });
        });
        return doc.GeneratePdf();
    }

    public async Task<byte[]> ExportClassReportExcelAsync(Guid classId, Guid requesterId, string role)
    {
        var @class = await _db.Classes.Include(c => c.Trainer).FirstOrDefaultAsync(c => c.Id == classId)
            ?? throw new Exception("Class not found.");

        var enrolledIds = await _db.ClassEnrollments
            .Include(e => e.Employee)
            .Where(e => e.ClassId == classId && e.Status == EnrollmentStatus.Active && e.Employee.Role == UserRole.Employee)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        var employees = await _db.Users
            .Where(u => enrolledIds.Contains(u.Id))
            .OrderBy(u => u.Name)
            .ToListAsync();

        var tasks = await _db.Tasks
            .Where(t => t.ClassId == classId)
            .OrderBy(t => t.StartAt)
            .ToListAsync();

        var taskIds = tasks.Select(t => t.Id).ToList();
        var submissions = await _db.Submissions
            .Where(s => taskIds.Contains(s.TaskId) && s.Status != SubmissionStatus.Draft)
            .ToListAsync();

        using var wb = new XLWorkbook();

        // ── Sheet 1: Summary ───────────────────────────────────────────────────
        var ws1 = wb.Worksheets.Add("Summary");
        ws1.Cell("A1").Value = "Class"; ws1.Cell("B1").Value = @class.Name;
        ws1.Cell("A2").Value = "Trainer"; ws1.Cell("B2").Value = @class.Trainer.Name;
        ws1.Cell("A3").Value = "Employees"; ws1.Cell("B3").Value = employees.Count;
        ws1.Cell("A4").Value = "Tasks"; ws1.Cell("B4").Value = tasks.Count;
        var allScores = submissions.Where(s => s.TotalFinalScore.HasValue).Select(s => (double)s.TotalFinalScore!.Value).ToList();
        ws1.Cell("A5").Value = "Overall Avg Score"; ws1.Cell("B5").Value = allScores.Any() ? Math.Round(allScores.Average(), 1) : 0;
        var totalPossible = tasks.Count * employees.Count;
        ws1.Cell("A6").Value = "Completion Rate"; ws1.Cell("B6").Value = totalPossible > 0 ? $"{Math.Round((double)submissions.Count / totalPossible * 100, 1)}%" : "0%";
        ws1.Columns().AdjustToContents();

        // ── Sheet 2: Student × Task Matrix ────────────────────────────────────
        var ws2 = wb.Worksheets.Add("Student Scores");
        ws2.Cell(1, 1).Value = "Employee";
        for (int c = 0; c < tasks.Count; c++)
        {
            ws2.Cell(1, c + 2).Value = tasks[c].Title;
            ws2.Cell(2, c + 2).Value = $"(/ {tasks[c].TotalMarks})";
        }
        ws2.Cell(1, tasks.Count + 2).Value = "Total";
        ws2.Cell(1, tasks.Count + 3).Value = "Avg %";

        // Use GroupBy + First to handle edge cases where an employee has more than one
        // non-draft submission for the same task (e.g. Submitted + Malpractice).
        var subLookup = submissions
            .GroupBy(s => (s.EmployeeId, s.TaskId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.SubmittedAt ?? DateTime.MinValue).First());
        for (int r = 0; r < employees.Count; r++)
        {
            var emp = employees[r];
            ws2.Cell(r + 3, 1).Value = emp.Name;
            double totalScore = 0; int totalMaxScore = 0;
            for (int c = 0; c < tasks.Count; c++)
            {
                if (subLookup.TryGetValue((emp.Id, tasks[c].Id), out var sub))
                {
                    var score = (double)(sub.TotalFinalScore ?? sub.TotalRawScore ?? 0);
                    ws2.Cell(r + 3, c + 2).Value = score;
                    totalScore += score; totalMaxScore += tasks[c].TotalMarks;
                }
                else
                {
                    ws2.Cell(r + 3, c + 2).Value = "—";
                }
            }
            ws2.Cell(r + 3, tasks.Count + 2).Value = totalScore;
            ws2.Cell(r + 3, tasks.Count + 3).Value = totalMaxScore > 0 ? $"{Math.Round(totalScore / totalMaxScore * 100, 1)}%" : "—";
        }

        // Task averages row
        int avgRow = employees.Count + 3;
        ws2.Cell(avgRow, 1).Value = "Class Average";
        for (int c = 0; c < tasks.Count; c++)
        {
            var taskSubs = submissions.Where(s => s.TaskId == tasks[c].Id && s.TotalFinalScore.HasValue).ToList();
            ws2.Cell(avgRow, c + 2).Value = taskSubs.Any() ? Math.Round(taskSubs.Average(s => (double)s.TotalFinalScore!.Value), 1) : 0;
        }
        ws2.Columns().AdjustToContents();

        // ── Sheet 3: Task Breakdown ────────────────────────────────────────────
        var ws3 = wb.Worksheets.Add("Task Breakdown");
        ws3.Cell(1, 1).Value = "Task"; ws3.Cell(1, 2).Value = "Total Marks";
        ws3.Cell(1, 3).Value = "Submitted"; ws3.Cell(1, 4).Value = "Not Submitted";
        ws3.Cell(1, 5).Value = "Avg Raw Score"; ws3.Cell(1, 6).Value = "Avg Final Score";
        ws3.Cell(1, 7).Value = "Completion %";
        for (int r = 0; r < tasks.Count; r++)
        {
            var t = tasks[r];
            var taskSubs = submissions.Where(s => s.TaskId == t.Id).ToList();
            var notSub = employees.Count - taskSubs.Count;
            var rawScores = taskSubs.Where(s => s.TotalRawScore.HasValue).Select(s => (double)s.TotalRawScore!.Value).ToList();
            var finScores = taskSubs.Where(s => s.TotalFinalScore.HasValue).Select(s => (double)s.TotalFinalScore!.Value).ToList();
            ws3.Cell(r + 2, 1).Value = t.Title;
            ws3.Cell(r + 2, 2).Value = t.TotalMarks;
            ws3.Cell(r + 2, 3).Value = taskSubs.Count;
            ws3.Cell(r + 2, 4).Value = notSub;
            ws3.Cell(r + 2, 5).Value = rawScores.Any() ? Math.Round(rawScores.Average(), 1) : 0;
            ws3.Cell(r + 2, 6).Value = finScores.Any() ? Math.Round(finScores.Average(), 1) : 0;
            ws3.Cell(r + 2, 7).Value = employees.Count > 0 ? $"{Math.Round((double)taskSubs.Count / employees.Count * 100, 1)}%" : "0%";
        }
        ws3.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportTaskResultsGridExcelAsync(Guid taskId)
    {
        var task = await _db.Tasks
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .Include(t => t.Class)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new Exception("Task not found.");

        var submissions = await _db.Submissions
            .Include(s => s.Answers)
            .Include(s => s.Employee)
            .Where(s => s.TaskId == taskId && s.Status != SubmissionStatus.Draft)
            .OrderBy(s => s.SubmissionRank ?? int.MaxValue)
            .ToListAsync();

        var questions = task.Questions.OrderBy(q => q.OrderIndex).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results Grid");

        // Header row
        ws.Cell(1, 1).Value = "Rank";
        ws.Cell(1, 2).Value = "Student";
        ws.Cell(1, 3).Value = "Status";
        ws.Cell(1, 4).Value = "Raw Score";
        ws.Cell(1, 5).Value = "Final Score";
        ws.Cell(1, 6).Value = "Multiplier";
        ws.Cell(1, 7).Value = "Accuracy %";
        for (int c = 0; c < questions.Count; c++)
            ws.Cell(1, c + 8).Value = $"Q{c + 1} ({questions[c].Marks}m)";

        // Style header
        var headerRange = ws.Range(1, 1, 1, 7 + questions.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
        headerRange.Style.Font.FontColor = XLColor.White;

        // Data rows
        for (int r = 0; r < submissions.Count; r++)
        {
            var sub = submissions[r];
            var totalMarks = questions.Sum(q => q.Marks);
            int rawPts = 0;

            for (int c = 0; c < questions.Count; c++)
            {
                var q = questions[c];
                var ans = sub.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
                double score = 0;
                if (q.Type == Domain.Enums.QuestionType.MCQ && ans != null)
                {
                    var correctOpt = q.Options.FirstOrDefault(o => o.IsCorrect);
                    score = correctOpt != null && ans.AnswerText == correctOpt.Id.ToString() ? q.Marks : 0;
                }
                else if (ans?.RawScore != null)
                    score = (double)ans.RawScore.Value;

                ws.Cell(r + 2, c + 8).Value = score;
                rawPts += (int)score;
            }

            var accuracy = totalMarks > 0 ? Math.Round((double)rawPts / totalMarks * 100, 1) : 0;
            ws.Cell(r + 2, 1).Value = sub.SubmissionRank ?? r + 1;
            ws.Cell(r + 2, 2).Value = sub.Employee?.Name ?? "Unknown";
            ws.Cell(r + 2, 3).Value = sub.Status.ToString();
            ws.Cell(r + 2, 4).Value = (double)(sub.TotalRawScore ?? rawPts);
            ws.Cell(r + 2, 5).Value = (double)(sub.TotalFinalScore ?? 0);
            ws.Cell(r + 2, 6).Value = (double)(sub.Multiplier ?? 1);
            ws.Cell(r + 2, 7).Value = accuracy;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportEmployeeReportPdfAsync(Guid employeeId, Guid requesterId, string role)
    {
        var report = await GetEmployeeReportAsync(employeeId, requesterId, role);
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Text($"Performance Report: {report.EmployeeName}").SemiBold().FontSize(18);
                page.Content().Column(col =>
                {
                    col.Item().Text($"Class: {report.ClassName} | XP: {report.TotalXP} | Avg Score: {report.AvgFinalScore:F1}");
                    col.Item().PaddingTop(10).Text("Task History").SemiBold().FontSize(14);
                    foreach (var t in report.TaskResults)
                        col.Item().PaddingTop(4).Text($"{t.TaskTitle}: Score {t.FinalScore:F1} | Rank #{t.Rank ?? 0} | Multiplier {t.Multiplier:P0}");
                });
                page.Footer().Text(x => { x.Span("Generated by SSPMS | "); x.CurrentPageNumber(); });
            });
        });
        return doc.GeneratePdf();
    }
}
