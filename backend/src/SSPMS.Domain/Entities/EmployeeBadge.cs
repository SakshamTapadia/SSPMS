using SSPMS.Domain.Common;

namespace SSPMS.Domain.Entities;

public class EmployeeBadge : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid BadgeId { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Employee { get; set; } = null!;
    public Badge Badge { get; set; } = null!;
}
