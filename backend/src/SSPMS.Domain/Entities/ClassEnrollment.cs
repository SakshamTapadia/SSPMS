using SSPMS.Domain.Common;
using SSPMS.Domain.Enums;

namespace SSPMS.Domain.Entities;

public class ClassEnrollment : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid ClassId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    // Navigation
    public User Employee { get; set; } = null!;
    public Class Class { get; set; } = null!;
}
