using Microsoft.EntityFrameworkCore;
using SSPMS.Application.DTOs.Analytics;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Enums;
using SSPMS.Infrastructure.Data;

namespace SSPMS.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public AnalyticsService(ApplicationDbContext db) => _db = db;

    // ── Blind Spot Analysis ────────────────────────────────────────────────────
    public async Task<TaskBlindSpotReport> GetTaskBlindSpotsAsync(Guid taskId)
    {
        var task = await _db.Tasks.Include(t => t.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new Exception("Task not found.");

        var submissions = await _db.Submissions
            .Include(s => s.Answers)
            .Where(s => s.TaskId == taskId && s.Status != SubmissionStatus.Draft)
            .ToListAsync();

        var totalSubs = submissions.Count;

        var questionSpots = task.Questions.OrderBy(q => q.OrderIndex).Select(q =>
        {
            var answers = submissions.SelectMany(s => s.Answers).Where(a => a.QuestionId == q.Id).ToList();
            var answered = answers.Count;
            int correct;
            double avgScorePct;

            if (q.Type == QuestionType.MCQ)
            {
                var correctOptionId = q.Options.FirstOrDefault(o => o.IsCorrect)?.Id.ToString();
                correct = answers.Count(a => a.AnswerText == correctOptionId);
                avgScorePct = answered > 0 ? (double)correct / answered * 100 : 0;
            }
            else
            {
                // For subjective: use raw scores
                var scored = answers.Where(a => a.RawScore.HasValue).ToList();
                correct = scored.Count(a => a.RawScore >= q.Marks * 0.5m); // pass = 50% of marks
                avgScorePct = scored.Any() ? (double)scored.Average(a => (double)a.RawScore!.Value) / q.Marks * 100 : 0;
            }

            var passRate = answered > 0 ? (double)correct / answered : 0;

            return new QuestionBlindSpot(
                q.Id, q.Stem, q.Type.ToString(), q.Marks, q.OrderIndex,
                answered, correct,
                Math.Round(passRate, 3),
                Math.Round(avgScorePct, 1),
                passRate < 0.5 && answered > 0
            );
        }).ToList();

        return new TaskBlindSpotReport(task.Id, task.Title, totalSubs, questionSpots);
    }

    // ── Code Similarity ────────────────────────────────────────────────────────
    public async Task<CodeSimilarityReport> GetCodeSimilarityAsync(Guid taskId)
    {
        var task = await _db.Tasks.Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new Exception("Task not found.");

        var codeQuestionIds = task.Questions
            .Where(q => q.Type == QuestionType.Code)
            .Select(q => q.Id)
            .ToHashSet();

        if (!codeQuestionIds.Any())
            return new CodeSimilarityReport(taskId, task.Title, [], []);

        var submissions = await _db.Submissions
            .Include(s => s.Answers)
            .Include(s => s.Employee)
            .Where(s => s.TaskId == taskId && s.Status != SubmissionStatus.Draft)
            .ToListAsync();

        // Aggregate all code answers per submission
        var submissionCodes = submissions.Select(s => new
        {
            s.Id,
            EmployeeName = s.Employee?.Name ?? "Unknown",
            Code = string.Join("\n\n", s.Answers
                .Where(a => codeQuestionIds.Contains(a.QuestionId) && !string.IsNullOrWhiteSpace(a.AnswerText))
                .Select(a => a.AnswerText))
        }).Where(x => !string.IsNullOrWhiteSpace(x.Code)).ToList();

        var pairs = new List<SimilarityEntry>();

        for (int i = 0; i < submissionCodes.Count; i++)
        {
            for (int j = i + 1; j < submissionCodes.Count; j++)
            {
                var sim = JaccardSimilarity(submissionCodes[i].Code!, submissionCodes[j].Code!);
                pairs.Add(new SimilarityEntry(
                    submissionCodes[i].Id, submissionCodes[i].EmployeeName,
                    submissionCodes[j].Id, submissionCodes[j].EmployeeName,
                    Math.Round(sim, 3),
                    sim > 0.72
                ));
            }
        }

        // Cluster suspected pairs using union-find
        var clusters = BuildClusters(pairs.Where(p => p.IsSuspected).ToList(), submissionCodes.Select(s => (s.Id, s.EmployeeName)).ToList());

        return new CodeSimilarityReport(taskId, task.Title, pairs.OrderByDescending(p => p.Similarity).ToList(), clusters);
    }

    private static double JaccardSimilarity(string a, string b)
    {
        var tokensA = Tokenize(a);
        var tokensB = Tokenize(b);
        if (tokensA.Count == 0 && tokensB.Count == 0) return 1.0;
        if (tokensA.Count == 0 || tokensB.Count == 0) return 0.0;
        var intersection = tokensA.Intersect(tokensB).Count();
        var union = tokensA.Union(tokensB).Count();
        return union == 0 ? 0.0 : (double)intersection / union;
    }

    private static HashSet<string> Tokenize(string code)
    {
        // Split on whitespace and punctuation, lowercase, length > 2
        return new HashSet<string>(
            code.Split([' ', '\n', '\r', '\t', '{', '}', '(', ')', ';', ',', '.', '[', ']', ':', '"', '\''], StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToLowerInvariant())
                .Where(t => t.Length > 2)
        );
    }

    private static List<SimilarityCluster> BuildClusters(List<SimilarityEntry> suspected, List<(Guid Id, string Name)> all)
    {
        var parent = all.ToDictionary(x => x.Id, x => x.Id);

        Guid Find(Guid x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
        void Union(Guid x, Guid y) { var px = Find(x); var py = Find(y); if (px != py) parent[px] = py; }

        foreach (var p in suspected) { if (parent.ContainsKey(p.SubmissionAId) && parent.ContainsKey(p.SubmissionBId)) Union(p.SubmissionAId, p.SubmissionBId); }

        var groups = all.GroupBy(x => Find(x.Id)).Where(g => g.Count() > 1).Select((g, i) =>
        {
            var memberIds = g.Select(x => x.Id).ToHashSet();
            var groupPairs = suspected.Where(p => memberIds.Contains(p.SubmissionAId) && memberIds.Contains(p.SubmissionBId)).ToList();
            var avgSim = groupPairs.Any() ? groupPairs.Average(p => p.Similarity) : 0.0;
            var risk = avgSim >= 0.90 ? "High" : avgSim >= 0.80 ? "Medium" : "Low";
            return new SimilarityCluster(i + 1, g.Select(x => x.Name).ToList(), Math.Round(avgSim, 3), risk);
        }).ToList();

        return groups;
    }

    // ── Velocity ───────────────────────────────────────────────────────────────
    public async Task<EmployeeVelocityDto> GetEmployeeVelocityAsync(Guid employeeId)
    {
        var user = await _db.Users.FindAsync(employeeId) ?? throw new Exception("User not found.");

        var recentSubs = await _db.Submissions
            .Include(s => s.Task)
            .Where(s => s.EmployeeId == employeeId && s.Status != SubmissionStatus.Draft && s.SubmittedAt != null && s.Task.TotalMarks > 0)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(6)
            .ToListAsync();

        var dataPoints = recentSubs.Select(s => new ScoreDataPoint(
            s.TaskId, s.Task.Title, s.SubmittedAt!.Value,
            (double)(s.TotalFinalScore ?? 0),
            s.Task.TotalMarks
        )).OrderBy(p => p.SubmittedAt).ToList();

        var velocity = ComputeVelocity(dataPoints);
        var trend = velocity > 3 ? "Rising" : velocity < -3 ? "Falling" : "Stable";
        var predicted = PredictNext(dataPoints);

        return new EmployeeVelocityDto(employeeId, user.Name, dataPoints, Math.Round(velocity, 1), trend, Math.Round(predicted, 1));
    }

    public async Task<ClassVelocityReport> GetClassVelocityAsync(Guid classId)
    {
        var cls = await _db.Classes.FindAsync(classId) ?? throw new Exception("Class not found.");
        var employeeIds = await _db.ClassEnrollments
            .Where(e => e.ClassId == classId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        var velocities = new List<EmployeeVelocityDto>();
        foreach (var empId in employeeIds)
        {
            try { velocities.Add(await GetEmployeeVelocityAsync(empId)); } catch { }
        }

        return new ClassVelocityReport(classId, cls.Name, velocities.OrderByDescending(v => v.VelocityPercent).ToList());
    }

    private static double ComputeVelocity(List<ScoreDataPoint> points)
    {
        if (points.Count < 2) return 0;
        // Linear regression slope on normalized scores (as % of total marks)
        var scores = points.Select(p => p.TotalMarks > 0 ? p.FinalScore / p.TotalMarks * 100 : 0).ToList();
        int n = scores.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        for (int i = 0; i < n; i++) { sumX += i; sumY += scores[i]; sumXY += i * scores[i]; sumX2 += i * i; }
        double denom = n * sumX2 - sumX * sumX;
        if (denom == 0) return 0;
        return (n * sumXY - sumX * sumY) / denom; // slope per task
    }

    private static double PredictNext(List<ScoreDataPoint> points)
    {
        if (points.Count < 2) return points.Any() ? points.Last().FinalScore / points.Last().TotalMarks * 100 : 0;
        var scores = points.Select(p => p.TotalMarks > 0 ? p.FinalScore / p.TotalMarks * 100 : 0).ToList();
        int n = scores.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        for (int i = 0; i < n; i++) { sumX += i; sumY += scores[i]; sumXY += i * scores[i]; sumX2 += i * i; }
        double denom = n * sumX2 - sumX * sumX;
        if (denom == 0) return scores.Last();
        double slope = (n * sumXY - sumX * sumY) / denom;
        double intercept = (sumY - slope * sumX) / n;
        return Math.Clamp(intercept + slope * n, 0, 100);
    }

    // ── Results Grid ────────────────────────────────────────────────────────────
    public async Task<TaskResultsGrid> GetTaskResultsGridAsync(Guid taskId)
    {
        var task = await _db.Tasks
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .Include(t => t.Class).ThenInclude(c => c.Enrollments)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new Exception("Task not found.");

        var submissions = await _db.Submissions
            .Include(s => s.Answers)
            .Include(s => s.Employee)
            .Where(s => s.TaskId == taskId && s.Status != SubmissionStatus.Draft)
            .ToListAsync();

        var questions = task.Questions.OrderBy(q => q.OrderIndex).ToList();
        var enrolled = task.Class?.Enrollments.Count(e => e.Status == Domain.Enums.EnrollmentStatus.Active) ?? 0;
        var totalMarks = questions.Sum(q => q.Marks);

        // Per-question correct answer counts
        var questionAccuracies = new Dictionary<Guid, (int correct, int total)>();
        foreach (var q in questions) questionAccuracies[q.Id] = (0, 0);

        var participantRows = new List<GridParticipantRow>();

        foreach (var sub in submissions.OrderBy(s => s.SubmissionRank ?? int.MaxValue))
        {
            var cells = new List<GridAnswerCell>();
            int totalPoints = 0;

            foreach (var q in questions)
            {
                var answer = sub.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
                bool? isCorrect = null;
                double? rawScore = answer?.RawScore;
                double maxScore = q.Marks;

                if (q.Type == Domain.Enums.QuestionType.MCQ && answer != null)
                {
                    var correctOption = q.Options.FirstOrDefault(o => o.IsCorrect);
                    if (correctOption != null)
                    {
                        isCorrect = answer.AnswerText == correctOption.Id.ToString();
                        rawScore = isCorrect == true ? q.Marks : 0;
                    }
                }
                else if (answer?.RawScore != null)
                {
                    isCorrect = answer.RawScore > 0;
                }

                if (isCorrect.HasValue)
                {
                    var (c, t) = questionAccuracies[q.Id];
                    questionAccuracies[q.Id] = (isCorrect == true ? c + 1 : c, t + 1);
                }

                totalPoints += (int)(rawScore ?? 0);
                cells.Add(new GridAnswerCell(q.Id, isCorrect, rawScore, maxScore));
            }

            var accuracy = totalMarks > 0 ? Math.Round((double)totalPoints / totalMarks * 100, 0) : 0;
            var score = sub.TotalFinalScore.HasValue ? (long)sub.TotalFinalScore.Value : (long)totalPoints * 100;

            participantRows.Add(new GridParticipantRow(
                sub.EmployeeId, sub.Employee?.Name ?? "Unknown",
                totalPoints, totalMarks, accuracy, score, cells));
        }

        var questionHeaders = questions.Select(q =>
        {
            var (correct, total) = questionAccuracies[q.Id];
            var acc = total > 0 ? Math.Round((double)correct / total * 100, 0) : 0;
            return new GridQuestionHeader(q.Id, q.OrderIndex, q.Stem, q.Type.ToString(), q.Marks, acc);
        }).ToList();

        var overallCorrect = questionAccuracies.Values.Sum(v => v.correct);
        var overallTotal = questionAccuracies.Values.Sum(v => v.total);
        var overallAccuracy = overallTotal > 0 ? Math.Round((double)overallCorrect / overallTotal * 100, 0) : 0;
        var participationRate = enrolled > 0 ? Math.Round((double)submissions.Count / enrolled * 100, 0) : 100;

        return new TaskResultsGrid(
            taskId, task.Title,
            enrolled, submissions.Count,
            overallAccuracy, participationRate,
            questions.Count,
            questionHeaders, participantRows);
    }
}
