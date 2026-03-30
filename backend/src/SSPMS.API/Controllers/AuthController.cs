using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SSPMS.Application.DTOs.Auth;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Enums;

namespace SSPMS.API.Controllers;

[Route("api/v1/auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _auth;
    private readonly IEmailService _email;
    public AuthController(IAuthService auth, IEmailService email) { _auth = auth; _email = email; }

    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request, CurrentUserIp);
        if (!result.Succeeded) return BadRequest(new { message = result.Error });
        if (result.Data!.RequiresEmailVerification)
            return Ok(new { requiresVerification = true, email = result.Data.Email });
        return Ok(result.Data.AuthData);
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request, CurrentUserIp);
        if (!result.Succeeded)
        {
            if (result.Error == "EMAIL_NOT_VERIFIED")
                return Ok(new { requiresVerification = true, email = request.Email });
            return Unauthorized(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpPost("verify-email"), AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var result = await _auth.VerifyEmailAsync(request.Email, request.Otp);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPost("resend-verification"), AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordRequest request)
    {
        await _auth.ResendEmailVerificationAsync(request.Email);
        return Ok(new { message = "Verification code sent if the email is registered and unverified." });
    }

    [HttpPost("google"), AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var result = await _auth.GoogleLoginAsync(request.IdToken, CurrentUserIp);
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
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthController>>();
        try
        {
            await _auth.ForgotPasswordAsync(request.Email);
            return Ok(new { message = "OTP sent to your email." });
        }
        catch (Exception ex)
        {
            // OTP was saved to DB but email delivery failed — let the frontend know so user can retry.
            logger.LogError(ex, "Email delivery failed for forgot-password: {Email}", request.Email);
            return StatusCode(503, new { message = "OTP generated but email delivery failed. Please try again or contact support." });
        }
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

    /// <summary>Admin-only: sends a test email to verify SMTP config.</summary>
    [HttpPost("test-email"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> TestEmail([FromQuery] string? to)
    {
        var target = to ?? CurrentUserEmail;
        try
        {
            await _email.SendAsync(target, "SSPMS SMTP Test",
                "<h2>SMTP test passed!</h2><p>If you see this, email delivery is working correctly.</p>");
            return Ok(new { message = $"Test email sent to {target}." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "SMTP failed.", error = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}
