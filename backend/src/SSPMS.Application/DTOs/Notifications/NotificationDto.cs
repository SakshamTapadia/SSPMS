using SSPMS.Domain.Enums;

namespace SSPMS.Application.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    NotificationType Type,
    bool IsRead,
    DateTime CreatedAt
);

public record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    Guid CreatedByUserId,
    string CreatedByName,
    Guid? ClassId,
    string? ClassName,
    DateTime CreatedAt
);

public record CreateAnnouncementRequest(
    string Title,
    string Body,
    Guid? ClassId = null
);
