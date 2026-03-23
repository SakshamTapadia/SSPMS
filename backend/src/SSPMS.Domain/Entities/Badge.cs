using SSPMS.Domain.Common;

namespace SSPMS.Domain.Entities;

public class Badge : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string Criteria { get; set; } = string.Empty; // JSON

    // Navigation
    public ICollection<EmployeeBadge> EmployeeBadges { get; set; } = new List<EmployeeBadge>();
}
