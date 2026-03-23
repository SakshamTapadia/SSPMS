using SSPMS.Domain.Common;

namespace SSPMS.Domain.Entities;

public class Class : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? SkillTags { get; set; }
    public Guid TrainerId { get; set; }
    public bool IsArchived { get; set; } = false;

    // Navigation
    public User Trainer { get; set; } = null!;
    public ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
    public ICollection<AssignedTask> Tasks { get; set; } = new List<AssignedTask>();
    public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
}
