using SSPMS.Domain.Common;

namespace SSPMS.Domain.Entities;

public class SubmissionAnswer : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public Guid QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public decimal? RawScore { get; set; }
    public decimal? FinalScore { get; set; }
    public string? EvaluatorNote { get; set; }
    public bool IsPlagiarismFlag { get; set; } = false;

    // Navigation
    public Submission Submission { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
