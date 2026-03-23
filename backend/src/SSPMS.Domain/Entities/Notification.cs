using SSPMS.Domain.Common;
using SSPMS.Domain.Enums;

namespace SSPMS.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;

    // Navigation
    public User User { get; set; } = null!;
}
