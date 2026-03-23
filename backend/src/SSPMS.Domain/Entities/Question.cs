using SSPMS.Domain.Common;
using SSPMS.Domain.Enums;

namespace SSPMS.Domain.Entities;

public class Question : BaseEntity
{
    public Guid TaskId { get; set; }
    public QuestionType Type { get; set; }
    public string Stem { get; set; } = string.Empty;
    public int Marks { get; set; }
    public int OrderIndex { get; set; }
    public string? Language { get; set; }         // Code: 'csharp' | 'javascript' | 'python'
    public string? ExpectedOutput { get; set; }   // Code: trainer reference only

    // Navigation
    public AssignedTask Task { get; set; } = null!;
    public ICollection<MCQOption> Options { get; set; } = new List<MCQOption>();
    public ICollection<SubmissionAnswer> Answers { get; set; } = new List<SubmissionAnswer>();
}
