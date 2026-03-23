namespace SSPMS.Domain.Entities;

public class PracticeProblem
{
    public Guid Id { get; set; } // Also serves as FK to PreparationMaterial
    public string Language { get; set; } = string.Empty; // csharp | javascript | python
    public string? StarterCode { get; set; }
    public string SampleSolution { get; set; } = string.Empty;
    public string TestCases { get; set; } = string.Empty; // JSON array: [{input, expectedOutput}]

    // Navigation
    public PreparationMaterial Material { get; set; } = null!;
    public ICollection<EmployeePracticeAttempt> Attempts { get; set; } = new List<EmployeePracticeAttempt>();
}
