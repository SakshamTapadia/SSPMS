using Microsoft.EntityFrameworkCore;
using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Notifications;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Entities;
using SSPMS.Domain.Enums;
using SSPMS.Infrastructure.Data;

namespace SSPMS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ISignalRService _signalR;

    public NotificationService(ApplicationDbContext db, ISignalRService signalR)
    {
        _db = db;
        _signalR = signalR;
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page, int pageSize)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync();
    }

    public async Task<ServiceResult> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
        if (n == null) return ServiceResult.Failure("Not found.");
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(Guid userId)
    {
        await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return ServiceResult.Success();
    }

    public async Task SendNotificationAsync(Guid userId, string title, string body, NotificationType type)
    {
        var notification = new Notification { UserId = userId, Title = title, Body = body, Type = type };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        await _signalR.SendNotificationToUserAsync(userId, new NotificationDto(notification.Id, title, body, type, false, notification.CreatedAt));
    }

    public async Task SendClassNotificationAsync(Guid classId, string title, string body, NotificationType type)
    {
        var employeeIds = await _db.ClassEnrollments
            .Where(e => e.ClassId == classId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        foreach (var userId in employeeIds)
            await SendNotificationAsync(userId, title, body, type);
    }

    public async Task<IEnumerable<AnnouncementDto>> GetAnnouncementsAsync(Guid userId, Guid? classId)
    {
        var user = await _db.Users.FindAsync(userId);
        return await _db.Announcements
            .Include(a => a.CreatedBy)
            .Include(a => a.Class)
            .Where(a => a.ClassId == null || a.ClassId == classId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.CreatedByUserId, a.CreatedBy.Name, a.ClassId, a.Class != null ? a.Class.Name : null, a.CreatedAt))
            .ToListAsync();
    }

    public async Task<ServiceResult<AnnouncementDto>> CreateAnnouncementAsync(CreateAnnouncementRequest request, Guid createdById)
    {
        var announcement = new Announcement
        {
            CreatedByUserId = createdById,
            ClassId = request.ClassId,
            Title = request.Title,
            Body = request.Body
        };
        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();

        if (request.ClassId.HasValue)
            await SendClassNotificationAsync(request.ClassId.Value, $"Announcement: {request.Title}", request.Body, NotificationType.Announcement);

        var creator = await _db.Users.FindAsync(createdById);
        var dto = new AnnouncementDto(announcement.Id, announcement.Title, announcement.Body, createdById, creator!.Name, request.ClassId, null, announcement.CreatedAt);
        return ServiceResult<AnnouncementDto>.Success(dto);
    }
}
