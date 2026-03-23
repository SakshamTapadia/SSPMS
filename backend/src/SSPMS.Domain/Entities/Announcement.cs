using SSPMS.Domain.Common;

namespace SSPMS.Domain.Entities;

public class Announcement : BaseEntity
{
    public Guid CreatedByUserId { get; set; }
    public Guid? ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Navigation
    public User CreatedBy { get; set; } = null!;
    public Class? Class { get; set; }
}
