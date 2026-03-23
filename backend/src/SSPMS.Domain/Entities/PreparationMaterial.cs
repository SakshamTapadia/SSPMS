namespace SSPMS.Domain.Entities;

public class PreparationMaterial
{
    public Guid Id { get; set; }
    public Guid? ClassId { get; set; } // Nullable for system-wide materials
    public string SkillTag { get; set; } = string.Empty;
    public MaterialType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentUrl { get; set; } // File path or external URL
    public Guid CreatedByTrainerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPublished { get; set; }

    // Navigation properties
    public Class? Class { get; set; }
    public User CreatedByTrainer { get; set; } = null!;
    public ICollection<PracticeProblem> PracticeProblems { get; set; } = new List<PracticeProblem>();
}

public enum MaterialType
{
    StudyGuide = 0,
    PracticeQuiz = 1,
    CodeChallenge = 2,
    VideoLecture = 3,
    Reference = 4,
    ExternalLink = 5
}
