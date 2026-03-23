using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SSPMS.Application.Interfaces;

namespace SSPMS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("SSPMS", _config["Email:From"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = WrapTemplate(subject, htmlBody) };

        using var client = new SmtpClient();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        await client.ConnectAsync(_config["Email:SmtpHost"], int.Parse(_config["Email:SmtpPort"] ?? "587"), MailKit.Security.SecureSocketOptions.StartTls, cts.Token);
        await client.AuthenticateAsync(_config["Email:From"], _config["Email:Password"], cts.Token);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public Task SendWelcomeEmailAsync(string to, string name, string tempPassword) =>
        SendAsync(to, "Welcome to SSPMS — Your Account is Ready",
            $"<h2>Welcome, {name}!</h2><p>Your account has been created on the SmartSkill Performance Monitoring System.</p><p><strong>Email:</strong> {to}<br/><strong>Temporary Password:</strong> {tempPassword}</p><p>Please log in and change your password immediately.</p>");

    public Task SendOtpEmailAsync(string to, string name, string otp) =>
        SendAsync(to, "SSPMS — Password Reset OTP",
            $"<h2>Password Reset</h2><p>Hi {name},</p><p>Your OTP for password reset is: <strong style='font-size:24px;letter-spacing:4px'>{otp}</strong></p><p>This code expires in 15 minutes.</p>");

    public Task SendTaskAssignedEmailAsync(string to, string name, string taskTitle, DateTime startAt, DateTime endAt) =>
        SendAsync(to, $"New Task Assigned: {taskTitle}",
            $"<h2>New Task: {taskTitle}</h2><p>Hi {name},</p><p>A new task has been assigned to you.</p><p><strong>Opens:</strong> {startAt:dd MMM yyyy HH:mm} UTC<br/><strong>Closes:</strong> {endAt:dd MMM yyyy HH:mm} UTC</p><p>Log in to SSPMS to view and attempt the task.</p>");

    public Task SendTaskEvaluatedEmailAsync(string to, string name, string taskTitle, decimal finalScore) =>
        SendAsync(to, $"Results Ready: {taskTitle}",
            $"<h2>Your results are ready!</h2><p>Hi {name},</p><p>Your submission for <strong>{taskTitle}</strong> has been evaluated.</p><p><strong>Final Score:</strong> {finalScore}</p><p>Log in to SSPMS to view detailed feedback.</p>");

    public Task SendDeadlineReminderEmailAsync(string to, string name, string taskTitle, DateTime endAt) =>
        SendAsync(to, $"Reminder: {taskTitle} closes in 1 hour",
            $"<h2>Deadline Reminder</h2><p>Hi {name},</p><p><strong>{taskTitle}</strong> closes at <strong>{endAt:HH:mm} UTC</strong> — that's in about 1 hour.</p><p>Make sure you've submitted your answers!</p>");

    public Task SendBadgeEarnedEmailAsync(string to, string name, string badgeName, string badgeDescription) =>
        SendAsync(to, $"You earned a badge: {badgeName}",
            $"<h2>🏅 New Badge Unlocked!</h2><p>Congratulations, {name}!</p><p>You earned the <strong>{badgeName}</strong> badge.</p><p><em>{badgeDescription}</em></p>");

    public Task SendWeeklyDigestAsync(string to, string trainerName, string className, object digestData) =>
        SendAsync(to, $"Weekly Performance Digest — {className}",
            $"<h2>Weekly Digest: {className}</h2><p>Hi {trainerName},</p><p>Here is your weekly performance summary for <strong>{className}</strong>. Log in to SSPMS for the full report.</p>");

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
