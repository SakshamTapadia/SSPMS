namespace SSPMS.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendWelcomeEmailAsync(string to, string name, string tempPassword);
    Task SendOtpEmailAsync(string to, string name, string otp);
    Task SendTaskAssignedEmailAsync(string to, string name, string taskTitle, DateTime startAt, DateTime endAt);
    Task SendTaskEvaluatedEmailAsync(string to, string name, string taskTitle, decimal finalScore);
    Task SendDeadlineReminderEmailAsync(string to, string name, string taskTitle, DateTime endAt);
    Task SendBadgeEarnedEmailAsync(string to, string name, string badgeName, string badgeDescription);
    Task SendWeeklyDigestAsync(string to, string trainerName, string className, object digestData);
}
