namespace SSPMS.Application.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,      // seconds until expiry
    UserProfileDto User
);

public record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string? AvatarUrl,
    bool IsTwoFactorEnabled
);
