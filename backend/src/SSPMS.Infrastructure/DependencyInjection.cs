using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SSPMS.Application.Interfaces;
using SSPMS.Infrastructure.Data;
using SSPMS.Infrastructure.Services;

namespace SSPMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                    npgsqlOptions.CommandTimeout(30);
                }));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IGamificationService, GamificationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddHttpClient();
        if (!string.IsNullOrWhiteSpace(configuration["Cloudinary:CloudName"]) &&
            !string.IsNullOrWhiteSpace(configuration["Cloudinary:UploadPreset"]))
            services.AddScoped<IImageService, CloudinaryImageService>();
        if (!string.IsNullOrWhiteSpace(configuration["Brevo:ApiKey"]))
            services.AddScoped<IEmailService, BrevoEmailService>();
        else if (!string.IsNullOrWhiteSpace(configuration["Resend:ApiKey"]))
            services.AddScoped<IEmailService, ResendEmailService>();
        else
            services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISignalRService, SignalRService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}
