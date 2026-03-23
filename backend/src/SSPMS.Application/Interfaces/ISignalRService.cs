namespace SSPMS.Application.Interfaces;

public interface ISignalRService
{
    Task SendNotificationToUserAsync(Guid userId, object notification);
    Task SendSubmissionCountUpdateAsync(Guid taskId, int count);
    Task SendAnnouncementToClassAsync(Guid classId, object announcement);
}
