using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Notifications;
using SSPMS.Domain.Enums;

namespace SSPMS.Application.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page, int pageSize);
    Task<ServiceResult> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<ServiceResult> MarkAllAsReadAsync(Guid userId);
    Task SendNotificationAsync(Guid userId, string title, string body, NotificationType type);
    Task SendClassNotificationAsync(Guid classId, string title, string body, NotificationType type);
    Task<IEnumerable<AnnouncementDto>> GetAnnouncementsAsync(Guid userId, Guid? classId);
    Task<ServiceResult<AnnouncementDto>> CreateAnnouncementAsync(CreateAnnouncementRequest request, Guid createdById);
}
