namespace SSPMS.Domain.Entities;

public class EmployeePracticeAttempt
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid PracticeProblemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }

    // Navigation
    public User Employee { get; set; } = null!;
    public PracticeProblem PracticeProblem { get; set; } = null!;
}
