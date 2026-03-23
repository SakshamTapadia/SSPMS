using SSPMS.Domain.Common;
using SSPMS.Domain.Enums;

namespace SSPMS.Domain.Entities;

public class AssignedTask : BaseEntity
{
    public Guid ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int TotalMarks { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int DurationMinutes { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;
    public Guid CreatedByTrainerId { get; set; }
    public bool IsOpenBook { get; set; } // true = external resources allowed during attempt

    // Navigation
    public Class Class { get; set; } = null!;
    public User CreatedByTrainer { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
