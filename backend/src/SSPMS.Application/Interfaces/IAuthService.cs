using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Auth;

namespace SSPMS.Application.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, string ipAddress);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress);
    Task<ServiceResult<AuthResponse>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<ServiceResult> LogoutAsync(string refreshToken);
    Task<ServiceResult> ForgotPasswordAsync(string email);
    Task<ServiceResult> VerifyOtpAsync(VerifyOtpRequest request);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<ServiceResult<string>> Enable2FAAsync(Guid userId);
    Task<ServiceResult> Verify2FAAsync(Guid userId, string totpCode);
    Task<ServiceResult> Disable2FAAsync(Guid userId, string password);
    Task<ServiceResult<AuthResponse>> VerifyEmailAsync(string email, string otp);
    Task<ServiceResult> ResendEmailVerificationAsync(string email);
    Task<ServiceResult<AuthResponse>> GoogleLoginAsync(string idToken, string ipAddress);
}
