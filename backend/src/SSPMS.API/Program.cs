using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SSPMS.API.Middleware;
using SSPMS.Infrastructure.Services;
using SSPMS.Application.Interfaces;
using SSPMS.Infrastructure;
using SSPMS.Infrastructure.Data;
using SSPMS.Domain.Entities;
using SSPMS.Domain.Enums;
using BC = BCrypt.Net.BCrypt;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ─────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();

// ─── Swagger ──────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SSPMS API", Version = "v1", Description = "SmartSkill Performance Monitoring System API" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste your JWT access token here (no 'Bearer ' prefix needed — Swagger adds it automatically).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
        Array.Empty<string>()
    }});
});

// ─── JWT Authentication ───────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ─── SignalR ──────────────────────────────────────────────────────────
var signalRBuilder = builder.Services.AddSignalR(opts => opts.EnableDetailedErrors = builder.Environment.IsDevelopment());
var redisConn = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConn))
    signalRBuilder.AddStackExchangeRedis(redisConn);

// ─── CORS ─────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(builder.Configuration["AllowedOrigins"]?.Split(',') ?? ["http://localhost:4200"])
              .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ─── Rate Limiting ────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("auth", o => { o.Window = TimeSpan.FromMinutes(1); o.PermitLimit = 10; }));

// ─── Infrastructure ───────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ─── Background Job ───────────────────────────────────────────────────
builder.Services.AddHostedService<ExpiredSubmissionsJob>();

var app = builder.Build();

// ─── Auto-migrate + seed on startup ──────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Bootstrap: ensure sakshamtapadia02@gmail.com exists as admin
    const string adminEmail = "sakshamtapadia02@gmail.com";
    var existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
    if (existingAdmin == null)
    {
        db.Users.Add(new User
        {
            Name = "Saksham Tapadia",
            Email = adminEmail,
            PasswordHash = BC.HashPassword("Admin@1234"),
            Role = UserRole.Admin,
            IsActive = true,
            IsEmailVerified = true
        });
    }
    else
    {
        // Only sync email if no other user already owns it (prevent unique constraint crash)
        var emailTaken = await db.Users.AnyAsync(u => u.Email == adminEmail && u.Id != existingAdmin.Id);
        if (!emailTaken) existingAdmin.Email = adminEmail;
        existingAdmin.Name = "Saksham Tapadia";
        existingAdmin.IsActive = true;
        existingAdmin.IsEmailVerified = true;
    }
    await db.SaveChangesAsync();

    // ── Dev seed: 1 extra admin, 2 trainers, 50 employees ────────────────
    // Runs automatically whenever there are no non-admin users in the DB.
    var nonAdminCount = await db.Users.CountAsync(u => u.Role != UserRole.Admin);
    if (nonAdminCount == 0)
        await SeedDevDataAsync(db);
}

// ─── Middleware pipeline ──────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SSPMS API v1"));

if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// ─── Background service: process expired task submissions every minute ─
public class ExpiredSubmissionsJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredSubmissionsJob> _logger;

    public ExpiredSubmissionsJob(IServiceScopeFactory scopeFactory, ILogger<ExpiredSubmissionsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var submissionService = scope.ServiceProvider.GetRequiredService<ISubmissionService>();
                await submissionService.ProcessExpiredSubmissionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired submissions.");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

public partial class Program
{
    static async Task SeedDevDataAsync(ApplicationDbContext db)
    {
        // Clear dependent tables in FK-safe order before inserting users
        await db.PasswordResetOTPs.ExecuteDeleteAsync();
        await db.RefreshTokens.ExecuteDeleteAsync();
        await db.AuditLogs.ExecuteDeleteAsync();
        await db.Notifications.ExecuteDeleteAsync();
        await db.XPLedger.ExecuteDeleteAsync();
        await db.EmployeeBadges.ExecuteDeleteAsync();
        await db.SubmissionAnswers.ExecuteDeleteAsync();
        await db.Submissions.ExecuteDeleteAsync();
        await db.MCQOptions.ExecuteDeleteAsync();
        await db.Questions.ExecuteDeleteAsync();
        await db.Announcements.ExecuteDeleteAsync();
        await db.ClassEnrollments.ExecuteDeleteAsync();
        await db.Tasks.ExecuteDeleteAsync();
        await db.Classes.ExecuteDeleteAsync();
        await db.Users.Where(u => u.Role != UserRole.Admin).ExecuteDeleteAsync();

        var adminPw   = BC.HashPassword("Admin@1234");
        var trainerPw = BC.HashPassword("Trainer@1234");
        var empPw     = BC.HashPassword("Employee@1234");

        // ── Second admin ────────────────────────────────────────────────
        db.Users.Add(new User
        {
            Name = "Benhar Charles", Email = "benhar.charles@sspms.com",
            PasswordHash = adminPw, Role = UserRole.Admin,
            IsActive = true, IsEmailVerified = true
        });

        // ── 2 Trainers ──────────────────────────────────────────────────
        db.Users.AddRange(
            new User { Name = "Priya Sharma",  Email = "priya.sharma@sspms.com",  PasswordHash = trainerPw, Role = UserRole.Trainer, IsActive = true, IsEmailVerified = true },
            new User { Name = "Rahul Verma",   Email = "rahul.verma@sspms.com",   PasswordHash = trainerPw, Role = UserRole.Trainer, IsActive = true, IsEmailVerified = true }
        );

        // ── 50 Employees ─────────────────────────────────────────────────
        var employeeNames = new[]
        {
            "Aarav Kumar",     "Arjun Singh",      "Vivek Patel",      "Rohit Sharma",    "Amit Gupta",
            "Karan Mehta",     "Nikhil Joshi",     "Siddharth Nair",   "Akash Yadav",     "Pranav Malhotra",
            "Suresh Iyer",     "Deepak Rajput",    "Vijay Chaudhary",  "Ankit Pandey",    "Harish Tiwari",
            "Manish Bose",     "Rajesh Mishra",    "Vishal Agarwal",   "Gaurav Srivastava","Ravi Chatterjee",
            "Naveen Pillai",   "Sanjay Rao",       "Ashish Banerjee",  "Mohit Dixit",     "Piyush Kumar",
            "Ananya Sharma",   "Pooja Verma",      "Sneha Patel",      "Divya Singh",     "Neha Gupta",
            "Riya Mehta",      "Prerna Joshi",     "Swati Nair",       "Kavya Yadav",     "Ishita Malhotra",
            "Meera Iyer",      "Tanvi Rajput",     "Sakshi Chaudhary", "Nisha Pandey",    "Preeti Tiwari",
            "Shruti Bose",     "Ankita Mishra",    "Komal Agarwal",    "Simran Srivastava","Payal Chatterjee",
            "Madhuri Pillai",  "Archana Rao",      "Deepika Banerjee", "Ritika Dixit",    "Sunita Kumar"
        };

        var employees = new List<User>();
        for (int i = 0; i < employeeNames.Length; i++)
        {
            var slug = employeeNames[i].ToLower().Replace(" ", ".");
            var emp = new User
            {
                Name = employeeNames[i],
                Email = $"{slug}@sspms.com",
                PasswordHash = empPw,
                Role = UserRole.Employee,
                IsActive = true,
                IsEmailVerified = true
            };
            db.Users.Add(emp);
            employees.Add(emp);
        }

        await db.SaveChangesAsync();

        // ── 2 Classes, one per trainer ───────────────────────────────────
        var trainers = await db.Users.Where(u => u.Role == UserRole.Trainer).ToListAsync();
        var class1 = new SSPMS.Domain.Entities.Class { Name = ".NET Developer Batch-T1", TrainerId = trainers[0].Id };
        var class2 = new SSPMS.Domain.Entities.Class { Name = ".NET Developer Batch-T2", TrainerId = trainers[1].Id };
        db.Classes.AddRange(class1, class2);
        await db.SaveChangesAsync();

        // ── Enroll first 25 employees in class1, next 25 in class2 ──────
        for (int i = 0; i < employees.Count; i++)
        {
            var classId = i < 25 ? class1.Id : class2.Id;
            db.ClassEnrollments.Add(new SSPMS.Domain.Entities.ClassEnrollment { EmployeeId = employees[i].Id, ClassId = classId });
        }
        await db.SaveChangesAsync();
    }
}
