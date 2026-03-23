using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SSPMS.Application.Interfaces;

namespace SSPMS.Infrastructure.Services;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public async Task JoinClassGroup(string classId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"class-{classId}");

    public async Task LeaveClassGroup(string classId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"class-{classId}");
}

[Authorize]
public class SubmissionHub : Hub
{
    public async Task JoinTaskGroup(string taskId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, taskId);

    public async Task LeaveTaskGroup(string taskId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, taskId);
}

public class SignalRService : ISignalRService
{
    private readonly IHubContext<NotificationHub> _notifHub;
    private readonly IHubContext<SubmissionHub> _submissionHub;

    public SignalRService(IHubContext<NotificationHub> notifHub, IHubContext<SubmissionHub> submissionHub)
    {
        _notifHub = notifHub;
        _submissionHub = submissionHub;
    }

    public async Task SendNotificationToUserAsync(Guid userId, object notification) =>
        await _notifHub.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notification);

    public async Task SendSubmissionCountUpdateAsync(Guid taskId, int count) =>
        await _submissionHub.Clients.Group(taskId.ToString()).SendAsync("SubmissionCountUpdated", new { taskId, count });

    public async Task SendAnnouncementToClassAsync(Guid classId, object announcement) =>
        await _notifHub.Clients.Group($"class-{classId}").SendAsync("ReceiveAnnouncement", announcement);
}
