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
        Description = "JWT Authorization. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

    // Bootstrap: create default admin if none exists
    if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
    {
        db.Users.Add(new User
        {
            Name = "System Admin",
            Email = "admin@sspms.com",
            PasswordHash = BC.HashPassword("Admin@1234"),
            Role = UserRole.Admin,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }
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

public partial class Program { }
