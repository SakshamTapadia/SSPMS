using SSPMS.Domain.Enums;

namespace SSPMS.Application.DTOs.Tasks;

public record TaskDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    string Title,
    string? Description,
    string? Instructions,
    int TotalMarks,
    DateTime StartAt,
    DateTime EndAt,
    int DurationMinutes,
    AssignmentStatus Status,
    Guid CreatedByTrainerId,
    string TrainerName,
    DateTime CreatedAt,
    int QuestionCount,
    int SubmissionCount
);

public record CreateTaskRequest(
    Guid ClassId,
    string Title,
    string? Description,
    string? Instructions,
    DateTime StartAt,
    DateTime EndAt,
    int DurationMinutes
);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    string? Instructions,
    DateTime StartAt,
    DateTime EndAt,
    int DurationMinutes
);

public record QuestionDto(
    Guid Id,
    Guid TaskId,
    QuestionType Type,
    string Stem,
    int Marks,
    int OrderIndex,
    string? Language,
    IEnumerable<MCQOptionDto>? Options
);

public record MCQOptionDto(
    Guid Id,
    string OptionText,
    int OrderIndex,
    bool? IsCorrect = null // Only set for trainer view
);

public record CreateQuestionRequest(
    QuestionType Type,
    string Stem,
    int Marks,
    int OrderIndex,
    string? Language,
    string? ExpectedOutput,
    IEnumerable<CreateMCQOptionRequest>? Options
);

public record CreateMCQOptionRequest(string OptionText, bool IsCorrect, int OrderIndex);
public record ReorderRequest(IEnumerable<Guid> QuestionIds);
