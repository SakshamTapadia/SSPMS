using SSPMS.Domain.Common;

namespace SSPMS.Domain.Entities;

public class MCQOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }

    // Navigation
    public Question Question { get; set; } = null!;
}
