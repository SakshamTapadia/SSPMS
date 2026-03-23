namespace SSPMS.Application.DTOs.Auth;

public record ForgotPasswordRequest(string Email);
public record VerifyOtpRequest(string Email, string Otp);
public record ResetPasswordRequest(string Email, string Otp, string NewPassword);
public record RefreshTokenRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
