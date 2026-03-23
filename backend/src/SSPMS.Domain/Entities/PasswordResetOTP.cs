using SSPMS.Domain.Common;

namespace SSPMS.Domain.Entities;

public class PasswordResetOTP : BaseEntity
{
    public Guid UserId { get; set; }
    public string OTPHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;

    // Navigation
    public User User { get; set; } = null!;
}
