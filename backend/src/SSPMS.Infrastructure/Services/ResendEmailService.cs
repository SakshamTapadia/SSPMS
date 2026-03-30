using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SSPMS.Application.Interfaces;

namespace SSPMS.Infrastructure.Services;

/// <summary>
/// Sends email via the Resend HTTP API (resend.com).
/// Set Resend__ApiKey in environment variables to activate.
/// Free tier: 3 000 emails/month, 100/day.
/// NOTE: With the sandbox "from" address (onboarding@resend.dev) you can only
/// send to your verified Resend account email. Add and verify your own domain
/// at resend.com/domains to send to any address.
/// </summary>
public class ResendEmailService : IEmailService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<ResendEmailService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var apiKey  = _config["Resend:ApiKey"]!;
        var fromAddr = _config["Email:From"] ?? "onboarding@resend.dev";
        var display  = _config["Email:DisplayName"] ?? "SSPMS";

        // Resend requires "Display Name <email>" format for custom domains.
        // The sandbox "onboarding@resend.dev" must be used as-is (no display name prefix).
        var fromField = fromAddr.Contains("resend.dev") ? fromAddr : $"{display} <{fromAddr}>";

        var payload = new
        {
            from    = fromField,
            to      = new[] { to },
            subject = subject,
            html    = WrapTemplate(subject, htmlBody)
        };

        using var client  = _httpFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Resend API returned {(int)response.StatusCode}: {body}");
        }

        _logger.LogInformation("Email sent via Resend to {To} — subject: {Subject}", to, subject);
    }

    public Task SendWelcomeEmailAsync(string to, string name, string tempPassword) =>
        SendAsync(to, "Welcome to SSPMS — Your Account is Ready",
            $"<h2>Welcome, {name}!</h2><p>Your account has been created on SSPMS.</p><p><strong>Email:</strong> {to}<br/><strong>Temporary Password:</strong> {tempPassword}</p><p>Please log in and change your password immediately.</p>");

    public Task SendEmailVerificationOtpAsync(string to, string name, string otp) =>
        SendAsync(to, "SSPMS — Verify Your Email Address",
            $"<h2>Verify Your Email</h2><p>Hi {name},</p><p>Your verification code:</p><p style='text-align:center'><strong style='font-size:32px;letter-spacing:6px;color:#4f46e5'>{otp}</strong></p><p>Expires in 15 minutes.</p>");

    public Task SendOtpEmailAsync(string to, string name, string otp) =>
        SendAsync(to, "SSPMS — Password Reset OTP",
            $"<h2>Password Reset</h2><p>Hi {name},</p><p>Your OTP: <strong style='font-size:24px;letter-spacing:4px'>{otp}</strong></p><p>Expires in 15 minutes.</p>");

    public Task SendTaskAssignedEmailAsync(string to, string name, string taskTitle, DateTime startAt, DateTime endAt) =>
        SendAsync(to, $"New Task Assigned: {taskTitle}",
            $"<h2>New Task: {taskTitle}</h2><p>Hi {name},</p><p><strong>Opens:</strong> {startAt:dd MMM yyyy HH:mm} UTC<br/><strong>Closes:</strong> {endAt:dd MMM yyyy HH:mm} UTC</p>");

    public Task SendTaskEvaluatedEmailAsync(string to, string name, string taskTitle, decimal finalScore) =>
        SendAsync(to, $"Results Ready: {taskTitle}",
            $"<h2>Results ready!</h2><p>Hi {name},</p><p><strong>{taskTitle}</strong> evaluated. Final Score: {finalScore}</p>");

    public Task SendDeadlineReminderEmailAsync(string to, string name, string taskTitle, DateTime endAt) =>
        SendAsync(to, $"Reminder: {taskTitle} closes in 1 hour",
            $"<h2>Deadline Reminder</h2><p>Hi {name},</p><p><strong>{taskTitle}</strong> closes at {endAt:HH:mm} UTC.</p>");

    public Task SendBadgeEarnedEmailAsync(string to, string name, string badgeName, string badgeDescription) =>
        SendAsync(to, $"You earned a badge: {badgeName}",
            $"<h2>New Badge!</h2><p>Congratulations {name}! You earned <strong>{badgeName}</strong>.</p><p>{badgeDescription}</p>");

    public Task SendWeeklyDigestAsync(string to, string trainerName, string className, object digestData) =>
        SendAsync(to, $"Weekly Performance Digest — {className}",
            $"<h2>Weekly Digest: {className}</h2><p>Hi {trainerName},</p><p>Log in to SSPMS for the full report.</p>");

    private static string WrapTemplate(string title, string body) =>
        "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"/><style>" +
        "body{font-family:Arial,sans-serif;background:#f5f5f5;margin:0;padding:0}" +
        ".container{max-width:600px;margin:40px auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.1)}" +
        ".header{background:#1976d2;color:#fff;padding:24px 32px}.header h1{margin:0;font-size:20px}" +
        ".body{padding:32px}.footer{background:#f5f5f5;text-align:center;padding:16px;font-size:12px;color:#888}" +
        "</style></head><body><div class=\"container\">" +
        "<div class=\"header\"><h1>SmartSkill Performance Monitoring System</h1></div>" +
        $"<div class=\"body\">{body}</div>" +
        "<div class=\"footer\">&copy; 2026 SSPMS. This is an automated message.</div>" +
        "</div></body></html>";
}
