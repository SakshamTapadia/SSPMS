namespace SSPMS.Application.DTOs.Auth;

public record RegisterResponse(
    bool RequiresEmailVerification,
    string? Email,
    AuthResponse? AuthData
);
