using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.DTOs.Notifications;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : BaseController
{
    private readonly INotificationService _notifications;
    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _notifications.GetUserNotificationsAsync(CurrentUserId, page, pageSize));

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var result = await _notifications.MarkAsReadAsync(id, CurrentUserId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllAsReadAsync(CurrentUserId);
        return NoContent();
    }

    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements()
        => Ok(await _notifications.GetAnnouncementsAsync(CurrentUserId, null));

    [HttpPost("announcements"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementRequest request)
    {
        var result = await _notifications.CreateAnnouncementAsync(request, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }
}
