namespace SSPMS.Application.DTOs.Gamification;

public record LeaderboardEntry(
    int Rank,
    Guid EmployeeId,
    string EmployeeName,
    string? AvatarUrl,
    int TotalXP,
    int TasksCompleted,
    decimal AvgFinalScore,
    string? ClassName
);

public record BadgeDto(
    Guid Id,
    string Name,
    string Description,
    string IconUrl
);

public record EmployeeBadgeDto(
    Guid BadgeId,
    string Name,
    string Description,
    string IconUrl,
    DateTime AwardedAt,
    int Count
);

public record XPSummaryDto(
    Guid EmployeeId,
    int TotalXP,
    int ClassRank,
    int GlobalRank,
    int CurrentStreak,
    IEnumerable<EmployeeBadgeDto> RecentBadges
);

public record DashboardStatsDto(
    int ClassRank,
    int GlobalRank,
    int TotalXP,
    int CurrentStreak,
    int TotalTasks,
    int SubmittedTasks,
    decimal AverageScore,
    IEnumerable<EmployeeBadgeDto> LatestBadges,
    IEnumerable<UpcomingTaskDto> UpcomingTasks
);

public record UpcomingTaskDto(
    Guid Id,
    string Title,
    DateTime StartAt,
    DateTime EndAt,
    int TotalMarks
);
