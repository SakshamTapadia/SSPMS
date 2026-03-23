using SSPMS.Domain.Enums;

namespace SSPMS.Application.DTOs.Submissions;

public record SubmissionDto(
    Guid Id,
    Guid TaskId,
    string TaskTitle,
    DateTime TaskEndAt,
    int TaskDurationMinutes,
    Guid EmployeeId,
    string EmployeeName,
    DateTime? StartedAt,
    DateTime? SubmittedAt,
    int? SubmissionRank,
    decimal? Multiplier,
    decimal? TotalRawScore,
    decimal? TotalFinalScore,
    SubmissionStatus Status,
    bool IsAutoSubmitted,
    bool IsMalpractice,
    int TabSwitchCount,
    IEnumerable<SubmissionAnswerDto> Answers
);

public record SubmissionAnswerDto(
    Guid Id,
    Guid QuestionId,
    string? AnswerText,
    decimal? RawScore,
    decimal? FinalScore,
    string? EvaluatorNote,
    bool IsPlagiarismFlag
);

public record StartSubmissionRequest(Guid TaskId);

public record SaveDraftRequest(IEnumerable<DraftAnswerItem> Answers);
public record DraftAnswerItem(Guid QuestionId, string? AnswerText);

public record EvaluateSubmissionRequest(IEnumerable<EvaluateAnswerItem> Answers);
public record EvaluateAnswerItem(Guid AnswerId, decimal RawScore, string? EvaluatorNote, bool IsPlagiarismFlag = false);

public record SetPlagiarismRequest(bool Flag);

public record MalpracticeSubmitRequest(int TabSwitchCount);

public record SubmissionSummary(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateTime? SubmittedAt,
    int? SubmissionRank,
    decimal? Multiplier,
    decimal? TotalRawScore,
    decimal? TotalFinalScore,
    SubmissionStatus Status
);
