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

        var enrolledIds = await _db.ClassEnrollments.Where(e => e.ClassId == classId && e.Status == EnrollmentStatus.Active).Select(e => e.EmployeeId).ToListAsync();
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
            var enrolled = _db.ClassEnrollments.Count(e => e.ClassId == c.Id && e.Status == EnrollmentStatus.Active);
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
        var report = await GetClassReportAsync(classId, requesterId, role);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Class Report");
        ws.Cell("A1").Value = "Task";
        ws.Cell("B1").Value = "Submissions";
        ws.Cell("C1").Value = "Avg Raw Score";
        ws.Cell("D1").Value = "Avg Final Score";
        ws.Cell("E1").Value = "Not Submitted";

        int row = 2;
        foreach (var t in report.TaskReports)
        {
            ws.Cell(row, 1).Value = t.TaskTitle;
            ws.Cell(row, 2).Value = t.SubmittedCount;
            ws.Cell(row, 3).Value = (double)t.AvgRawScore;
            ws.Cell(row, 4).Value = (double)t.AvgFinalScore;
            ws.Cell(row, 5).Value = t.NotSubmittedCount;
            row++;
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
