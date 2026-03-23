using Microsoft.EntityFrameworkCore;
using SSPMS.Domain.Entities;
using SSPMS.Domain.Enums;

namespace SSPMS.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();
    public DbSet<AssignedTask> Tasks => Set<AssignedTask>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<MCQOption> MCQOptions => Set<MCQOption>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionAnswer> SubmissionAnswers => Set<SubmissionAnswer>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetOTP> PasswordResetOTPs => Set<PasswordResetOTP>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<EmployeeBadge> EmployeeBadges => Set<EmployeeBadge>();
    public DbSet<XPLedger> XPLedger => Set<XPLedger>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<int>();
            e.Property(u => u.Name).HasMaxLength(200).IsRequired();
            e.Property(u => u.Email).HasMaxLength(300).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        });

        // Class
        modelBuilder.Entity<Class>(e =>
        {
            e.HasOne(c => c.Trainer)
             .WithMany(u => u.TrainerClasses)
             .HasForeignKey(c => c.TrainerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ClassEnrollment - one active enrollment per employee
        modelBuilder.Entity<ClassEnrollment>(e =>
        {
            e.Property(x => x.Status).HasConversion<int>();
            // Unique active enrollment enforced at application level (filtered index not portable)
        });

        // AssignedTask
        modelBuilder.Entity<AssignedTask>(e =>
        {
            e.ToTable("Tasks");
            e.Property(t => t.Status).HasConversion<int>();
            e.HasOne(t => t.Class)
             .WithMany(c => c.Tasks)
             .HasForeignKey(t => t.ClassId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.CreatedByTrainer)
             .WithMany()
             .HasForeignKey(t => t.CreatedByTrainerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Question
        modelBuilder.Entity<Question>(e =>
        {
            e.Property(q => q.Type).HasConversion<int>();
            e.HasOne(q => q.Task)
             .WithMany(t => t.Questions)
             .HasForeignKey(q => q.TaskId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // MCQOption
        modelBuilder.Entity<MCQOption>(e =>
        {
            e.HasOne(o => o.Question)
             .WithMany(q => q.Options)
             .HasForeignKey(o => o.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Submission
        modelBuilder.Entity<Submission>(e =>
        {
            e.Property(s => s.Status).HasConversion<int>();
            e.Property(s => s.Multiplier).HasPrecision(5, 2);
            e.Property(s => s.TotalRawScore).HasPrecision(10, 2);
            e.Property(s => s.TotalFinalScore).HasPrecision(10, 2);
            e.HasOne(s => s.Task)
             .WithMany(t => t.Submissions)
             .HasForeignKey(s => s.TaskId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Employee)
             .WithMany(u => u.Submissions)
             .HasForeignKey(s => s.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // SubmissionAnswer
        modelBuilder.Entity<SubmissionAnswer>(e =>
        {
            e.Property(a => a.RawScore).HasPrecision(10, 2);
            e.Property(a => a.FinalScore).HasPrecision(10, 2);
            e.HasOne(a => a.Submission)
             .WithMany(s => s.Answers)
             .HasForeignKey(a => a.SubmissionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Question)
             .WithMany(q => q.Answers)
             .HasForeignKey(a => a.QuestionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasOne(r => r.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PasswordResetOTP
        modelBuilder.Entity<PasswordResetOTP>(e =>
        {
            e.HasOne(p => p.User)
             .WithMany(u => u.PasswordResetOTPs)
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // XPLedger
        modelBuilder.Entity<XPLedger>(e =>
        {
            e.Property(x => x.Source).HasConversion<int>();
            e.HasOne(x => x.Employee)
             .WithMany(u => u.XPEntries)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        modelBuilder.Entity<Notification>(e =>
        {
            e.Property(n => n.Type).HasConversion<int>();
            e.HasOne(n => n.User)
             .WithMany(u => u.Notifications)
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Announcement
        modelBuilder.Entity<Announcement>(e =>
        {
            e.HasOne(a => a.CreatedBy)
             .WithMany()
             .HasForeignKey(a => a.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Class)
             .WithMany(c => c.Announcements)
             .HasForeignKey(a => a.ClassId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // Seed default badges — use a fixed UTC date to prevent migrations regenerating on every build
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Badge>().HasData(
            new Badge { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), Name = "Speed Demon", Description = "Submitted in the top 5 for any task", IconUrl = "/badges/speed-demon.svg", Criteria = "{\"type\":\"top_n_submission\",\"n\":5}", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Badge { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), Name = "Perfect Score", Description = "Achieved 100% raw score on any task", IconUrl = "/badges/perfect-score.svg", Criteria = "{\"type\":\"perfect_score\"}", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Badge { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), Name = "Consistent", Description = "Submitted on time for 5 consecutive tasks", IconUrl = "/badges/consistent.svg", Criteria = "{\"type\":\"consecutive_submissions\",\"count\":5}", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Badge { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), Name = "Streak Master", Description = "Maintained a 10-day activity streak", IconUrl = "/badges/streak-master.svg", Criteria = "{\"type\":\"streak\",\"days\":10}", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Badge { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), Name = "Top of Class", Description = "Ranked #1 on the class leaderboard for a week", IconUrl = "/badges/top-of-class.svg", Criteria = "{\"type\":\"weekly_top_rank\"}", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Badge { Id = Guid.Parse("11111111-0000-0000-0000-000000000006"), Name = "Early Bird", Description = "Submitted within the first 10 minutes of task opening", IconUrl = "/badges/early-bird.svg", Criteria = "{\"type\":\"early_submission\",\"minutes\":10}", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Badge { Id = Guid.Parse("11111111-0000-0000-0000-000000000007"), Name = "Comeback King", Description = "Improved score by 30%+ compared to previous task", IconUrl = "/badges/comeback.svg", Criteria = "{\"type\":\"score_improvement\",\"pct\":30}", CreatedAt = seedDate, UpdatedAt = seedDate }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
            {
                if (entry.State == EntityState.Modified)
                    entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
