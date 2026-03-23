namespace SSPMS.Application.DTOs.Classes;

public record ClassDto(
    Guid Id,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string? SkillTags,
    Guid TrainerId,
    string TrainerName,
    bool IsArchived,
    int EmployeeCount,
    int TaskCount,
    DateTime CreatedAt
);

public record CreateClassRequest(
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string? SkillTags,
    Guid TrainerId
);

public record UpdateClassRequest(
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string? SkillTags,
    Guid TrainerId
);

public record EnrollRequest(Guid EmployeeId);
public record TransferRequest(Guid EmployeeId, Guid TargetClassId);
