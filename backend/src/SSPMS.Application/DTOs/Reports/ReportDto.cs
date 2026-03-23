namespace SSPMS.Application.DTOs.Reports;

public record ClassReportDto(
    Guid ClassId,
    string ClassName,
    string TrainerName,
    int TotalEmployees,
    int TotalTasks,
    decimal AvgScore,
    decimal CompletionRate,
    IEnumerable<TaskReportItem> TaskReports,
    IEnumerable<DailyActivityItem> DailyActivity
);

public record TaskReportItem(
    Guid TaskId,
    string TaskTitle,
    DateTime StartAt,
    DateTime EndAt,
    int TotalMarks,
    int SubmittedCount,
    int NotSubmittedCount,
    decimal AvgRawScore,
    decimal AvgFinalScore,
    IEnumerable<ScoreBucket> ScoreDistribution
);

public record ScoreBucket(string Label, int Count);

public record DailyActivityItem(DateOnly Date, int SubmissionCount, decimal AvgScore);

public record EmployeeReportDto(
    Guid EmployeeId,
    string EmployeeName,
    string ClassName,
    int TotalXP,
    int ClassRank,
    int GlobalRank,
    decimal AvgFinalScore,
    IEnumerable<EmployeeTaskResult> TaskResults,
    IEnumerable<SkillTagScore> SkillScores,
    IEnumerable<DailyActivityItem> DailyActivity
);

public record EmployeeTaskResult(
    Guid TaskId,
    string TaskTitle,
    DateTime? SubmittedAt,
    int? Rank,
    decimal? RawScore,
    decimal? FinalScore,
    decimal? Multiplier,
    decimal? ClassAvgScore,
    decimal? ClassTopScore,
    string Status
);

public record SkillTagScore(string Tag, decimal AvgScore);

public record AdminSystemReportDto(
    int TotalUsers,
    int TotalTrainers,
    int TotalEmployees,
    int TotalClasses,
    int TotalTasks,
    int TotalSubmissions,
    IEnumerable<TrainerReportItem> TrainerSummaries,
    IEnumerable<ClassSummaryItem> ClassSummaries
);

public record TrainerReportItem(
    Guid TrainerId,
    string TrainerName,
    int ClassCount,
    int TaskCount,
    decimal AvgClassScore
);

public record ClassSummaryItem(
    Guid ClassId,
    string ClassName,
    string TrainerName,
    int EmployeeCount,
    int TaskCount,
    decimal AvgScore,
    decimal CompletionRate
);
