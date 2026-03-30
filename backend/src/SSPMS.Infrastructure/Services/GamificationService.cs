using Microsoft.EntityFrameworkCore;
using SSPMS.Application.DTOs.Gamification;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Entities;
using SSPMS.Domain.Enums;
using SSPMS.Infrastructure.Data;

namespace SSPMS.Infrastructure.Services;

public class GamificationService : IGamificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ISignalRService _signalR;
    private readonly INotificationService _notifications;

    private static readonly (int MaxRank, decimal Multiplier)[] Tiers =
    [
        (5,  1.00m),
        (10, 0.80m),
        (15, 0.60m),
        (20, 0.40m),
        (25, 0.20m),
    ];

    public GamificationService(ApplicationDbContext db, ISignalRService signalR, INotificationService notifications)
    {
        _db = db;
        _signalR = signalR;
        _notifications = notifications;
    }

    public decimal GetMultiplierForRank(int rank)
    {
        foreach (var (maxRank, multiplier) in Tiers)
            if (rank <= maxRank) return multiplier;
        return 0m;
    }

    public async Task<IEnumerable<LeaderboardEntry>> GetClassLeaderboardAsync(Guid classId, string period)
    {
        var from = GetPeriodStart(period);
        var enrolledIds = await _db.ClassEnrollments
            .Include(e => e.Employee)
            .Where(e => e.ClassId == classId && e.Status == EnrollmentStatus.Active && e.Employee.Role == UserRole.Employee)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        var query = _db.XPLedger
            .Where(x => enrolledIds.Contains(x.EmployeeId) && (from == null || x.CreatedAt >= from));

        var xpByUser = await query
            .GroupBy(x => x.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, TotalXP = g.Sum(x => x.Points) })
            .ToListAsync();

        var users = await _db.Users.Where(u => enrolledIds.Contains(u.Id)).ToListAsync();

        var submissions = await _db.Submissions
            .Where(s => enrolledIds.Contains(s.EmployeeId) && s.Status == SubmissionStatus.Evaluated)
            .ToListAsync();

        var className = (await _db.Classes.FindAsync(classId))?.Name ?? "";

        var entries = users.Select(u =>
        {
            var xp = xpByUser.FirstOrDefault(x => x.EmployeeId == u.Id)?.TotalXP ?? 0;
            var userSubs = submissions.Where(s => s.EmployeeId == u.Id).ToList();
            var avgScore = userSubs.Any() ? (decimal)userSubs.Average(s => (double)(s.TotalFinalScore ?? 0)) : 0;
            return new { User = u, XP = xp, TasksCompleted = userSubs.Count, AvgScore = avgScore };
        })
        .OrderByDescending(x => x.XP)
        .ThenByDescending(x => x.AvgScore)
        .Select((x, i) => new LeaderboardEntry(i + 1, x.User.Id, x.User.Name, x.User.AvatarUrl, x.XP, x.TasksCompleted, Math.Round(x.AvgScore, 1), className))
        .ToList();

        return entries;
    }

    public async Task<IEnumerable<LeaderboardEntry>> GetGlobalLeaderboardAsync(string period)
    {
        var from = GetPeriodStart(period);

        var allEmployees = await _db.Users.Where(u => u.Role == UserRole.Employee && u.IsActive).ToListAsync();

        var query = _db.XPLedger.Where(x => from == null || x.CreatedAt >= from);
        var xpByUser = await query
            .GroupBy(x => x.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, TotalXP = g.Sum(x => x.Points) })
            .ToListAsync();

        var submissions = await _db.Submissions
            .Where(s => s.Status == SubmissionStatus.Evaluated)
            .ToListAsync();

        var enrollments = await _db.ClassEnrollments
            .Include(e => e.Class)
            .Where(e => e.Status == EnrollmentStatus.Active)
            .ToListAsync();

        return allEmployees
            .Select(u =>
            {
                var xp = xpByUser.FirstOrDefault(x => x.EmployeeId == u.Id)?.TotalXP ?? 0;
                var userSubs = submissions.Where(s => s.EmployeeId == u.Id).ToList();
                var avgScore = userSubs.Any() ? (decimal)userSubs.Average(s => (double)(s.TotalFinalScore ?? 0)) : 0;
                var className = enrollments.FirstOrDefault(e => e.EmployeeId == u.Id)?.Class?.Name;
                return new { User = u, XP = xp, TasksCompleted = userSubs.Count, AvgScore = avgScore, ClassName = className };
            })
            .OrderByDescending(x => x.XP)
            .ThenByDescending(x => x.AvgScore)
            .Select((x, i) => new LeaderboardEntry(i + 1, x.User.Id, x.User.Name, x.User.AvatarUrl, x.XP, x.TasksCompleted, Math.Round(x.AvgScore, 1), x.ClassName))
            .ToList();
    }

    public async Task<IEnumerable<BadgeDto>> GetAllBadgesAsync()
    {
        return await _db.Badges.Select(b => new BadgeDto(b.Id, b.Name, b.Description, b.IconUrl)).ToListAsync();
    }

    public async Task<IEnumerable<EmployeeBadgeDto>> GetEmployeeBadgesAsync(Guid employeeId)
    {
        // Load to memory first — EF Core cannot translate GroupBy + navigation access (g.First().Badge.Name) to SQL
        var rows = await _db.EmployeeBadges
            .Include(eb => eb.Badge)
            .Where(eb => eb.EmployeeId == employeeId)
            .ToListAsync();

        return rows
            .GroupBy(eb => eb.BadgeId)
            .Select(g => new EmployeeBadgeDto(
                g.Key,
                g.First().Badge.Name,
                g.First().Badge.Description,
                g.First().Badge.IconUrl,
                g.Max(x => x.AwardedAt),
                g.Count()))
            .ToList();
    }

    public async Task<XPSummaryDto> GetXPSummaryAsync(Guid employeeId)
    {
        var totalXP = await _db.XPLedger.Where(x => x.EmployeeId == employeeId).SumAsync(x => x.Points);
        var classRank = await GetClassRankAsync(employeeId);
        var globalRank = await GetGlobalRankAsync(employeeId);
        var streak = await GetCurrentStreakAsync(employeeId);
        var badges = (await GetEmployeeBadgesAsync(employeeId)).Take(5);
        return new XPSummaryDto(employeeId, totalXP, classRank, globalRank, streak, badges);
    }

    public async Task<DashboardStatsDto> GetEmployeeDashboardAsync(Guid employeeId)
    {
        var xpSummary = await GetXPSummaryAsync(employeeId);
        var enrollment = await _db.ClassEnrollments.FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.Status == EnrollmentStatus.Active);

        var allSubs = await _db.Submissions
            .Include(s => s.Task)
            .Where(s => s.EmployeeId == employeeId && s.Status != SubmissionStatus.Draft)
            .ToListAsync();

        // Avg score as percentage of total marks for each task
        var scoredSubs = allSubs.Where(s => s.TotalFinalScore.HasValue && s.Task.TotalMarks > 0).ToList();
        var avgScore = scoredSubs.Any()
            ? scoredSubs.Average(s => (double)s.TotalFinalScore!.Value / s.Task.TotalMarks * 100)
            : 0;

        var upcoming = enrollment == null ? [] : await _db.Tasks
            .Where(t => t.ClassId == enrollment.ClassId && t.Status == Domain.Enums.AssignmentStatus.Published && t.StartAt > DateTime.UtcNow)
            .OrderBy(t => t.StartAt)
            .Take(3)
            .Select(t => new UpcomingTaskDto(t.Id, t.Title, t.StartAt, t.EndAt, t.TotalMarks))
            .ToListAsync();

        // Total tasks in the class (published or closed)
        var totalClassTasks = enrollment == null ? 0 : await _db.Tasks
            .CountAsync(t => t.ClassId == enrollment.ClassId && t.Status != Domain.Enums.AssignmentStatus.Draft);

        var badges = (await GetEmployeeBadgesAsync(employeeId)).Take(5).ToList();

        return new DashboardStatsDto(
            xpSummary.ClassRank, xpSummary.GlobalRank, xpSummary.TotalXP, xpSummary.CurrentStreak,
            totalClassTasks, allSubs.Count,
            (decimal)Math.Round(avgScore, 1),
            badges, upcoming);
    }

    public async Task ProcessBadgesForSubmissionAsync(Guid submissionId)
    {
        var submission = await _db.Submissions
            .Include(s => s.Task)
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission == null) return;

        var badges = await _db.Badges.ToListAsync();
        var awarded = new List<Badge>();

        // Speed Demon — top 5
        if (submission.SubmissionRank <= 5)
            awarded.Add(badges.First(b => b.Name == "Speed Demon"));

        // Early Bird — submitted within 10 minutes of task opening
        if (submission.SubmittedAt.HasValue && submission.SubmittedAt.Value <= submission.Task.StartAt.AddMinutes(10))
            awarded.Add(badges.First(b => b.Name == "Early Bird"));

        // Perfect Score — raw == total marks
        if (submission.TotalRawScore == submission.Task.TotalMarks)
            awarded.Add(badges.First(b => b.Name == "Perfect Score"));

        // Consecutive submissions — check last 4 tasks
        var employeeClassId = (await _db.ClassEnrollments.FirstOrDefaultAsync(e => e.EmployeeId == submission.EmployeeId && e.Status == EnrollmentStatus.Active))?.ClassId;
        if (employeeClassId.HasValue)
        {
            var recentTasks = await _db.Tasks
                .Where(t => t.ClassId == employeeClassId && t.Id != submission.TaskId && t.Status == Domain.Enums.AssignmentStatus.Closed)
                .OrderByDescending(t => t.EndAt)
                .Take(4)
                .Select(t => t.Id)
                .ToListAsync();
            var recentSubs = await _db.Submissions
                .Where(s => s.EmployeeId == submission.EmployeeId && recentTasks.Contains(s.TaskId) && s.SubmittedAt != null)
                .CountAsync();
            if (recentSubs >= 4)
                awarded.Add(badges.First(b => b.Name == "Consistent"));
        }

        // Comeback King — 30% improvement
        var prevSub = await _db.Submissions
            .Where(s => s.EmployeeId == submission.EmployeeId && s.Status == SubmissionStatus.Evaluated && s.Id != submissionId)
            .OrderByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync();
        if (prevSub != null && prevSub.TotalFinalScore.HasValue && submission.TotalFinalScore.HasValue)
        {
            var improvement = prevSub.TotalFinalScore > 0
                ? ((submission.TotalFinalScore - prevSub.TotalFinalScore) / prevSub.TotalFinalScore) * 100
                : 0;
            if (improvement >= 30)
                awarded.Add(badges.First(b => b.Name == "Comeback King"));
        }

        foreach (var badge in awarded)
        {
            _db.EmployeeBadges.Add(new EmployeeBadge { EmployeeId = submission.EmployeeId, BadgeId = badge.Id });
            _db.XPLedger.Add(new XPLedger { EmployeeId = submission.EmployeeId, Points = 20, Source = XPSource.Badge, ReferenceId = badge.Id });
            await _notifications.SendNotificationAsync(submission.EmployeeId, $"Badge Earned: {badge.Name}", badge.Description, NotificationType.BadgeEarned);
        }

        // XP for submission
        var baseXP = 50;
        var bonusXP = submission.SubmissionRank switch
        {
            <= 5 => 50,
            <= 10 => 30,
            <= 15 => 10,
            _ => 0
        };
        _db.XPLedger.Add(new XPLedger { EmployeeId = submission.EmployeeId, Points = baseXP + bonusXP, Source = XPSource.TaskSubmission, ReferenceId = submission.TaskId });

        await _db.SaveChangesAsync();
        await UpdateStreakAsync(submission.EmployeeId);
    }

    public async Task UpdateStreakAsync(Guid employeeId)
    {
        // Streak is calculated dynamically — no persistent field needed
        // Streak badge check: if current streak >= 10
        var streak = await GetCurrentStreakAsync(employeeId);
        if (streak >= 10)
        {
            var badge = await _db.Badges.FirstAsync(b => b.Name == "Streak Master");
            // Only award once per 10-day milestone
            var lastAward = await _db.EmployeeBadges
                .Where(eb => eb.EmployeeId == employeeId && eb.BadgeId == badge.Id)
                .OrderByDescending(eb => eb.AwardedAt)
                .FirstOrDefaultAsync();

            if (lastAward == null || (DateTime.UtcNow - lastAward.AwardedAt).TotalDays >= 10)
            {
                _db.EmployeeBadges.Add(new EmployeeBadge { EmployeeId = employeeId, BadgeId = badge.Id });
                _db.XPLedger.Add(new XPLedger { EmployeeId = employeeId, Points = 20, Source = XPSource.Streak });
                await _db.SaveChangesAsync();
                await _notifications.SendNotificationAsync(employeeId, "Badge Earned: Streak Master", "You maintained a 10-day streak!", NotificationType.BadgeEarned);
            }
        }
    }

    private async Task<int> GetCurrentStreakAsync(Guid employeeId)
    {
        var submissionDates = await _db.Submissions
            .Where(s => s.EmployeeId == employeeId && s.SubmittedAt != null)
            .Select(s => DateOnly.FromDateTime(s.SubmittedAt!.Value))
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        int streak = 0;
        var current = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var date in submissionDates)
        {
            if (date == current || date == current.AddDays(-1))
            { streak++; current = date; }
            else break;
        }
        return streak;
    }

    private async Task<int> GetClassRankAsync(Guid employeeId)
    {
        var enrollment = await _db.ClassEnrollments.FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.Status == EnrollmentStatus.Active);
        if (enrollment == null) return 0;
        var board = await GetClassLeaderboardAsync(enrollment.ClassId, "all");
        return board.FirstOrDefault(x => x.EmployeeId == employeeId)?.Rank ?? 0;
    }

    private async Task<int> GetGlobalRankAsync(Guid employeeId)
    {
        var board = await GetGlobalLeaderboardAsync("all");
        return board.FirstOrDefault(x => x.EmployeeId == employeeId)?.Rank ?? 0;
    }

    private static DateTime? GetPeriodStart(string period) => period switch
    {
        "week" => DateTime.UtcNow.AddDays(-7),
        "month" => DateTime.UtcNow.AddDays(-30),
        _ => null
    };
}
