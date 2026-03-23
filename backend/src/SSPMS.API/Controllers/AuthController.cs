using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.DTOs.Auth;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request, CurrentUserIp);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request, CurrentUserIp);
        return result.Succeeded ? Ok(result.Data) : Unauthorized(new { message = result.Error });
    }

    [HttpPost("refresh"), AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _auth.RefreshTokenAsync(request.RefreshToken, CurrentUserIp);
        return result.Succeeded ? Ok(result.Data) : Unauthorized(new { message = result.Error });
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _auth.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    [HttpPost("forgot-password"), AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _auth.ForgotPasswordAsync(request.Email);
        return Ok(new { message = "If that email exists, an OTP has been sent." });
    }

    [HttpPost("verify-otp"), AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var result = await _auth.VerifyOtpAsync(request);
        return result.Succeeded ? Ok(new { message = "OTP verified." }) : BadRequest(new { message = result.Error });
    }

    [HttpPost("reset-password"), AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _auth.ResetPasswordAsync(request);
        return result.Succeeded ? Ok(new { message = "Password reset successfully." }) : BadRequest(new { message = result.Error });
    }

    [HttpPut("password"), Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _auth.ChangePasswordAsync(CurrentUserId, request);
        return result.Succeeded ? Ok(new { message = "Password changed." }) : BadRequest(new { message = result.Error });
    }

    [HttpPost("2fa/enable"), Authorize]
    public async Task<IActionResult> Enable2FA()
    {
        var result = await _auth.Enable2FAAsync(CurrentUserId);
        return result.Succeeded ? Ok(new { qrUri = result.Data }) : BadRequest(new { message = result.Error });
    }

    [HttpPost("2fa/verify"), Authorize]
    public async Task<IActionResult> Verify2FA([FromBody] string totpCode)
    {
        var result = await _auth.Verify2FAAsync(CurrentUserId, totpCode);
        return result.Succeeded ? Ok(new { message = "2FA enabled." }) : BadRequest(new { message = result.Error });
    }

    [HttpPost("2fa/disable"), Authorize]
    public async Task<IActionResult> Disable2FA([FromBody] string password)
    {
        var result = await _auth.Disable2FAAsync(CurrentUserId, password);
        return result.Succeeded ? Ok(new { message = "2FA disabled." }) : BadRequest(new { message = result.Error });
    }
}
