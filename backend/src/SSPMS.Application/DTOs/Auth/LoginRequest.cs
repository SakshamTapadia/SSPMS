namespace SSPMS.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password, string? TotpCode = null);
