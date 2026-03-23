using SSPMS.Domain.Common;
using SSPMS.Domain.Enums;

namespace SSPMS.Domain.Entities;

public class Submission : BaseEntity
{
    public Guid TaskId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? SubmissionRank { get; set; }
    public decimal? Multiplier { get; set; }
    public decimal? TotalRawScore { get; set; }
    public decimal? TotalFinalScore { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Draft;
    public bool IsAutoSubmitted { get; set; } = false;
    public bool IsMalpractice { get; set; } = false;
    public int TabSwitchCount { get; set; } = 0;

    // Navigation
    public AssignedTask Task { get; set; } = null!;
    public User Employee { get; set; } = null!;
    public ICollection<SubmissionAnswer> Answers { get; set; } = new List<SubmissionAnswer>();
}
