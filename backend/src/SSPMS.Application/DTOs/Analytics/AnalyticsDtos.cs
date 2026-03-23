namespace SSPMS.Application.DTOs.Analytics;

// ── Blind Spot ─────────────────────────────────────────────
public record TaskBlindSpotReport(
    Guid TaskId,
    string TaskTitle,
    int TotalSubmissions,
    List<QuestionBlindSpot> Questions
);

public record QuestionBlindSpot(
    Guid QuestionId,
    string Stem,
    string Type,
    int Marks,
    int OrderIndex,
    int TotalAnswered,
    int CorrectAnswers,
    double PassRate,           // 0.0 – 1.0
    double AvgScorePercent,
    bool IsBlindSpot           // pass rate < 0.5
);

// ── Code Similarity ────────────────────────────────────────
public record CodeSimilarityReport(
    Guid TaskId,
    string TaskTitle,
    List<SimilarityEntry> Pairs,
    List<SimilarityCluster> Clusters
);

public record SimilarityEntry(
    Guid SubmissionAId,
    string EmployeeAName,
    Guid SubmissionBId,
    string EmployeeBName,
    double Similarity,         // 0.0 – 1.0
    bool IsSuspected           // similarity > 0.72
);

public record SimilarityCluster(
    int ClusterId,
    List<string> EmployeeNames,
    double AvgSimilarity,
    string RiskLevel            // "Low" | "Medium" | "High"
);

// ── Velocity ───────────────────────────────────────────────
public record EmployeeVelocityDto(
    Guid EmployeeId,
    string EmployeeName,
    List<ScoreDataPoint> RecentScores,   // last 6 tasks
    double VelocityPercent,              // slope as % change
    string Trend,                        // "Rising" | "Falling" | "Stable"
    double PredictedNextScore
);

public record ScoreDataPoint(
    Guid TaskId,
    string TaskTitle,
    DateTime SubmittedAt,
    double FinalScore,
    double TotalMarks
);

public record ClassVelocityReport(
    Guid ClassId,
    string ClassName,
    List<EmployeeVelocityDto> Employees
);
