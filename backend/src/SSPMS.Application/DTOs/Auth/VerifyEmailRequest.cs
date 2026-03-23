namespace SSPMS.Application.DTOs.Auth;

public record VerifyEmailRequest(string Email, string Otp);
