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

// ── Results Grid (Wayground-style overview) ────────────────
public record TaskResultsGrid(
    Guid TaskId,
    string TaskTitle,
    int TotalEnrolled,
    int TotalParticipants,
    double OverallAccuracy,       // 0–100
    double ParticipationRate,     // 0–100
    int QuestionCount,
    List<GridQuestionHeader> Questions,
    List<GridParticipantRow> Participants
);

public record GridQuestionHeader(
    Guid QuestionId,
    int OrderIndex,
    string Stem,
    string Type,
    int Marks,
    double AccuracyPercent        // % of participants who got this correct
);

public record GridParticipantRow(
    Guid EmployeeId,
    string EmployeeName,
    int TotalPoints,
    int TotalMarks,
    double AccuracyPercent,
    long Score,
    List<GridAnswerCell> Answers
);

public record GridAnswerCell(
    Guid QuestionId,
    bool? IsCorrect,             // null = not answered / subjective
    double? RawScore,
    double? MaxScore
);
