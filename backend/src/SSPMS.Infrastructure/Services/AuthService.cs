using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Auth;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Entities;
using SSPMS.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;

namespace SSPMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ApplicationDbContext db, IConfiguration config, IEmailService email, IAuditService audit, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _email = email;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, string ipAddress)
    {
        var emailLower = request.Email.ToLower().Trim();
        if (await _db.Users.AnyAsync(u => u.Email == emailLower))
            return ServiceResult<RegisterResponse>.Failure("An account with this email already exists.");

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = emailLower,
            PasswordHash = BC.HashPassword(request.Password),
            Role = Domain.Enums.UserRole.Employee,
            IsActive = true,
            IsEmailVerified = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Send verification OTP
        await SendVerificationOtpAsync(user);
        await _audit.LogAsync(user.Id, "Register.Success", ipAddress: ipAddress);

        return ServiceResult<RegisterResponse>.Success(
            new RegisterResponse(RequiresEmailVerification: true, Email: emailLower, AuthData: null));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);
        if (user == null || !BC.Verify(request.Password, user.PasswordHash))
        {
            await _audit.LogAsync(null, "Login.Failed", ipAddress: ipAddress);
            return ServiceResult<AuthResponse>.Failure("Invalid email or password.");
        }

        if (user.TwoFAEnabled)
        {
            if (string.IsNullOrEmpty(request.TotpCode))
                return ServiceResult<AuthResponse>.Failure("2FA code required.");
            if (!ValidateTotp(user.TwoFASecret!, request.TotpCode))
                return ServiceResult<AuthResponse>.Failure("Invalid 2FA code.");
        }

        var response = await GenerateAuthResponseAsync(user, ipAddress);
        await _audit.LogAsync(user.Id, "Login.Success", ipAddress: ipAddress);
        return ServiceResult<AuthResponse>.Success(response);
    }

    public async Task<ServiceResult<AuthResponse>> VerifyEmailAsync(string email, string otp)
    {
        var emailLower = email.ToLower().Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == emailLower);
        if (user == null) return ServiceResult<AuthResponse>.Failure("Invalid request.");

        var otps = await _db.PasswordResetOTPs
            .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var validOtp = otps.FirstOrDefault(o => BC.Verify(otp, o.OTPHash));
        if (validOtp == null) return ServiceResult<AuthResponse>.Failure("Invalid or expired verification code.");

        user.IsEmailVerified = true;
        validOtp.IsUsed = true;

        // Mark remaining OTPs as used
        foreach (var o in otps.Where(o => o.Id != validOtp.Id)) o.IsUsed = true;

        await _db.SaveChangesAsync();

        var response = await GenerateAuthResponseAsync(user, "verify");
        return ServiceResult<AuthResponse>.Success(response);
    }

    public async Task<ServiceResult> ResendEmailVerificationAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower() && u.IsActive && !u.IsEmailVerified);
        if (user == null) return ServiceResult.Success(); // Don't reveal if email exists
        await SendVerificationOtpAsync(user);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<AuthResponse>> GoogleLoginAsync(string idToken, string ipAddress)
    {
        var clientId = _config["Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            return ServiceResult<AuthResponse>.Failure("Google login is not configured.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] };
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch
        {
            return ServiceResult<AuthResponse>.Failure("Invalid Google token.");
        }

        var emailLower = payload.Email.ToLower().Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == emailLower);

        if (user == null)
        {
            // Auto-register via Google — email is already verified by Google
            user = new User
            {
                Name = payload.Name ?? emailLower,
                Email = emailLower,
                PasswordHash = BC.HashPassword(Guid.NewGuid().ToString()), // unusable random password
                Role = Domain.Enums.UserRole.Employee,
                IsActive = true,
                IsEmailVerified = true,
                GoogleId = payload.Subject,
                AvatarUrl = payload.Picture
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(user.Id, "Register.Google", ipAddress: ipAddress);
        }
        else
        {
            if (!user.IsActive)
                return ServiceResult<AuthResponse>.Failure("Account is deactivated.");

            // Update Google ID and avatar if not already set
            if (string.IsNullOrEmpty(user.GoogleId)) user.GoogleId = payload.Subject;
            if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
                user.AvatarUrl = payload.Picture;
            user.IsEmailVerified = true;
            await _db.SaveChangesAsync();
        }

        var response = await GenerateAuthResponseAsync(user, ipAddress);
        await _audit.LogAsync(user.Id, "Login.Google", ipAddress: ipAddress);
        return ServiceResult<AuthResponse>.Success(response);
    }

    public async Task<ServiceResult<AuthResponse>> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var tokens = await _db.RefreshTokens
            .Include(r => r.User)
            .Where(r => !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var stored = tokens.FirstOrDefault(r => BC.Verify(refreshToken, r.TokenHash));
        if (stored == null)
            return ServiceResult<AuthResponse>.Failure("Invalid or expired refresh token.");

        stored.IsRevoked = true;
        var response = await GenerateAuthResponseAsync(stored.User, ipAddress);
        await _db.SaveChangesAsync();
        return ServiceResult<AuthResponse>.Success(response);
    }

    public async Task<ServiceResult> LogoutAsync(string refreshToken)
    {
        var tokens = await _db.RefreshTokens.Where(r => !r.IsRevoked).ToListAsync();
        var stored = tokens.FirstOrDefault(r => BC.Verify(refreshToken, r.TokenHash));
        if (stored != null)
        {
            stored.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ForgotPasswordAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower() && u.IsActive);
        if (user == null) return ServiceResult.Success(); // Don't reveal if email exists

        // Clean up expired OTPs
        var expired = await _db.PasswordResetOTPs
            .Where(o => o.UserId == user.Id && (o.IsUsed || o.ExpiresAt <= DateTime.UtcNow))
            .ToListAsync();
        _db.PasswordResetOTPs.RemoveRange(expired);

        var otp = GenerateOtp();
        var otpHash = BC.HashPassword(otp);
        _db.PasswordResetOTPs.Add(new PasswordResetOTP
        {
            UserId = user.Id,
            OTPHash = otpHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
        await _db.SaveChangesAsync();
        try { await _email.SendOtpEmailAsync(user.Email, user.Name, otp); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send password reset OTP to {Email}", user.Email); throw; }
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());
        if (user == null) return ServiceResult.Failure("Invalid request.");

        var otps = await _db.PasswordResetOTPs
            .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var validOtp = otps.FirstOrDefault(o => BC.Verify(request.Otp, o.OTPHash));
        if (validOtp == null) return ServiceResult.Failure("Invalid or expired OTP.");

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());
        if (user == null) return ServiceResult.Failure("Invalid request.");

        var otps = await _db.PasswordResetOTPs
            .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var validOtp = otps.FirstOrDefault(o => BC.Verify(request.Otp, o.OTPHash));
        if (validOtp == null) return ServiceResult.Failure("Invalid or expired OTP.");

        user.PasswordHash = BC.HashPassword(request.NewPassword);
        validOtp.IsUsed = true;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null || !BC.Verify(request.CurrentPassword, user.PasswordHash))
            return ServiceResult.Failure("Current password is incorrect.");

        user.PasswordHash = BC.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<string>> Enable2FAAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ServiceResult<string>.Failure("User not found.");

        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretKey);
        user.TwoFASecret = base32Secret;
        await _db.SaveChangesAsync();

        var uri = $"otpauth://totp/SSPMS:{user.Email}?secret={base32Secret}&issuer=SSPMS";
        return ServiceResult<string>.Success(uri);
    }

    public async Task<ServiceResult> Verify2FAAsync(Guid userId, string totpCode)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.TwoFASecret))
            return ServiceResult.Failure("2FA not set up.");

        if (!ValidateTotp(user.TwoFASecret, totpCode))
            return ServiceResult.Failure("Invalid TOTP code.");

        user.TwoFAEnabled = true;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> Disable2FAAsync(Guid userId, string password)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null || !BC.Verify(password, user.PasswordHash))
            return ServiceResult.Failure("Password incorrect.");

        user.TwoFAEnabled = false;
        user.TwoFASecret = null;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task SendVerificationOtpAsync(User user)
    {
        // Clean up old OTPs for this user
        var old = await _db.PasswordResetOTPs
            .Where(o => o.UserId == user.Id && !o.IsUsed)
            .ToListAsync();
        _db.PasswordResetOTPs.RemoveRange(old);

        var otp = GenerateOtp();
        _db.PasswordResetOTPs.Add(new PasswordResetOTP
        {
            UserId = user.Id,
            OTPHash = BC.HashPassword(otp),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
        await _db.SaveChangesAsync();
        try { await _email.SendEmailVerificationOtpAsync(user.Email, user.Name, otp); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send verification OTP to {Email}", user.Email); }
        // Email failure is non-fatal — OTP is already saved in DB; user can request a resend.
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, string ipAddress)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60"));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenHash = BC.HashPassword(refreshTokenValue);

        // Clean up expired refresh tokens for this user
        var expiredTokens = await _db.RefreshTokens
            .Where(r => r.UserId == user.Id && (r.IsRevoked || r.ExpiresAt <= DateTime.UtcNow))
            .ToListAsync();
        _db.RefreshTokens.RemoveRange(expiredTokens);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        var expiresIn = (int)(expires - DateTime.UtcNow).TotalSeconds;
        var profile = new UserProfileDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.AvatarUrl, user.TwoFAEnabled);
        return new AuthResponse(accessToken, refreshTokenValue, expiresIn, profile);
    }

    private static string GenerateOtp() => Random.Shared.Next(100000, 999999).ToString();

    private static bool ValidateTotp(string secret, string code)
    {
        var bytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(bytes);
        return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
    }
}
