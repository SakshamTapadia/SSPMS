using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Users;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Entities;
using SSPMS.Domain.Enums;
using SSPMS.Infrastructure.Data;
using BC = BCrypt.Net.BCrypt;

namespace SSPMS.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;

    public UserService(ApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<PagedResult<UserListItem>> GetUsersAsync(int page, int pageSize, string? search, UserRole? role, bool? isActive)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(search)) query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
        if (role.HasValue) query = query.Where(u => u.Role == role);
        if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive);

        var total = await query.CountAsync();
        var items = await query.OrderBy(u => u.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserListItem(u.Id, u.Name, u.Email, u.Role, u.IsActive, null,
                _db.ClassEnrollments
                    .Where(e => e.EmployeeId == u.Id && e.Status == EnrollmentStatus.Active)
                    .Select(e => e.Class.Name)
                    .FirstOrDefault()))
            .ToListAsync();

        return new PagedResult<UserListItem> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return ServiceResult<UserDto>.Failure("User not found.");
        return ServiceResult<UserDto>.Success(Map(u));
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request, Guid createdBy)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            return ServiceResult<UserDto>.Failure("Email already in use.");

        var tempPassword = request.Password;
        var user = new User
        {
            Name = request.Name,
            Email = request.Email.ToLower(),
            PasswordHash = BC.HashPassword(tempPassword),
            Role = request.Role
        };
        _db.Users.Add(user);

        if (request.ClassId.HasValue && request.Role == UserRole.Employee)
            await EnrollAsync(user.Id, request.ClassId.Value);

        await _db.SaveChangesAsync();

        try { await _email.SendWelcomeEmailAsync(user.Email, user.Name, tempPassword); } catch { /* log */ }

        return ServiceResult<UserDto>.Success(Map(user));
    }

    public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return ServiceResult<UserDto>.Failure("User not found.");
        user.Name = request.Name;
        if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;
        await _db.SaveChangesAsync();
        return ServiceResult<UserDto>.Success(Map(user));
    }

    public async Task<ServiceResult> ChangeRoleAsync(Guid id, UserRole newRole)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return ServiceResult.Failure("User not found.");
        if (user.Role == UserRole.Admin) return ServiceResult.Failure("Cannot change the role of an Admin.");
        if (newRole == UserRole.Admin) return ServiceResult.Failure("Cannot promote a user to Admin.");
        user.Role = newRole;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeactivateUserAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return ServiceResult.Failure("User not found.");
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ReactivateUserAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return ServiceResult.Failure("User not found.");
        user.IsActive = true;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAvatarAsync(Guid id, string avatarUrl)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return ServiceResult.Failure("User not found.");
        user.AvatarUrl = avatarUrl;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<(int success, IEnumerable<string> errors)>> BulkImportAsync(Stream csv, Guid classId, Guid createdBy)
    {
        var errors = new List<string>();
        int success = 0;
        using var reader = new StreamReader(csv);
        // Simple CSV parsing: name,email
        string? line;
        int row = 0;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            row++;
            if (row == 1 && line.StartsWith("name", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = line.Split(',');
            if (parts.Length < 2) { errors.Add($"Row {row}: Invalid format."); continue; }
            var name = parts[0].Trim();
            var email = parts[1].Trim();
            if (await _db.Users.AnyAsync(u => u.Email == email.ToLower())) { errors.Add($"Row {row}: {email} already exists."); continue; }
            var tempPwd = Guid.NewGuid().ToString("N")[..8];
            var user = new User { Name = name, Email = email.ToLower(), PasswordHash = BC.HashPassword(tempPwd), Role = UserRole.Employee };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            await EnrollAsync(user.Id, classId);
            await _db.SaveChangesAsync();
            try { await _email.SendWelcomeEmailAsync(user.Email, user.Name, tempPwd); } catch { }
            success++;
        }
        return ServiceResult<(int, IEnumerable<string>)>.Success((success, errors));
    }

    public async Task<ServiceResult<UserDto>> CreateEmployeeByTrainerAsync(CreateUserRequest request, Guid trainerId)
    {
        Guid classId;

        if (request.ClassId.HasValue)
        {
            // Verify the trainer owns the specified class
            var owns = await _db.Classes.AnyAsync(c => c.Id == request.ClassId.Value && c.TrainerId == trainerId && !c.IsArchived);
            if (!owns) return ServiceResult<UserDto>.Failure("Class not found or access denied.");
            classId = request.ClassId.Value;
        }
        else
        {
            // Fall back to trainer's first active class
            var trainerClass = await _db.Classes.FirstOrDefaultAsync(c => c.TrainerId == trainerId && !c.IsArchived);
            if (trainerClass == null) return ServiceResult<UserDto>.Failure("You have no active class. Create a class first.");
            classId = trainerClass.Id;
        }

        var req = request with { ClassId = classId, Role = UserRole.Employee };
        return await CreateUserAsync(req, trainerId);
    }

    private async Task EnrollAsync(Guid employeeId, Guid classId)
    {
        // Archive any existing active enrollment
        await _db.ClassEnrollments
            .Where(e => e.EmployeeId == employeeId && e.Status == EnrollmentStatus.Active)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, EnrollmentStatus.Transferred));

        _db.ClassEnrollments.Add(new ClassEnrollment { EmployeeId = employeeId, ClassId = classId, Status = EnrollmentStatus.Active });
    }

    private static UserDto Map(User u) => new(u.Id, u.Name, u.Email, u.Role, u.AvatarUrl, u.IsActive, u.TwoFAEnabled, u.CreatedAt);
}
