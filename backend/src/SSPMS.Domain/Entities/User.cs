using SSPMS.Domain.Common;
using SSPMS.Domain.Enums;

namespace SSPMS.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = true;
    public string? GoogleId { get; set; }
    public bool TwoFAEnabled { get; set; } = false;
    public string? TwoFASecret { get; set; }

    // Navigation
    public ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
    public ICollection<Class> TrainerClasses { get; set; } = new List<Class>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<EmployeeBadge> Badges { get; set; } = new List<EmployeeBadge>();
    public ICollection<XPLedger> XPEntries { get; set; } = new List<XPLedger>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetOTP> PasswordResetOTPs { get; set; } = new List<PasswordResetOTP>();
}
