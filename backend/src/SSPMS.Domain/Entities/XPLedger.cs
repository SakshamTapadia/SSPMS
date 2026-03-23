using SSPMS.Domain.Common;
using SSPMS.Domain.Enums;

namespace SSPMS.Domain.Entities;

public class XPLedger : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public int Points { get; set; }
    public XPSource Source { get; set; }
    public Guid? ReferenceId { get; set; }

    // Navigation
    public User Employee { get; set; } = null!;
}
