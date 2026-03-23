using Microsoft.EntityFrameworkCore;
using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Classes;
using SSPMS.Application.DTOs.Users;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Entities;
using SSPMS.Domain.Enums;
using SSPMS.Infrastructure.Data;

namespace SSPMS.Infrastructure.Services;

public class ClassService : IClassService
{
    private readonly ApplicationDbContext _db;

    public ClassService(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<ClassDto>> GetClassesAsync(Guid? trainerId)
    {
        var query = _db.Classes.Include(c => c.Trainer).Include(c => c.Enrollments).Include(c => c.Tasks).AsQueryable();
        if (trainerId.HasValue) query = query.Where(c => c.TrainerId == trainerId);
        return await query.Select(c => MapDto(c)).ToListAsync();
    }

    public async Task<ServiceResult<ClassDto>> GetByIdAsync(Guid id, Guid requesterId, string role)
    {
        var c = await _db.Classes.Include(x => x.Trainer).Include(x => x.Enrollments).Include(x => x.Tasks).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return ServiceResult<ClassDto>.Failure("Class not found.");
        if (role == "Trainer" && c.TrainerId != requesterId) return ServiceResult<ClassDto>.Failure("Access denied.");
        return ServiceResult<ClassDto>.Success(MapDto(c));
    }

    public async Task<ServiceResult<ClassDto>> CreateClassAsync(CreateClassRequest request)
    {
        var trainer = await _db.Users.FindAsync(request.TrainerId);
        if (trainer == null || trainer.Role != UserRole.Trainer) return ServiceResult<ClassDto>.Failure("Trainer not found.");

        var @class = new Class
        {
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SkillTags = request.SkillTags,
            TrainerId = request.TrainerId
        };
        _db.Classes.Add(@class);
        await _db.SaveChangesAsync();
        await _db.Entry(@class).Reference(c => c.Trainer).LoadAsync();
        return ServiceResult<ClassDto>.Success(MapDto(@class));
    }

    public async Task<ServiceResult<ClassDto>> UpdateClassAsync(Guid id, UpdateClassRequest request)
    {
        var @class = await _db.Classes.Include(c => c.Trainer).FirstOrDefaultAsync(c => c.Id == id);
        if (@class == null) return ServiceResult<ClassDto>.Failure("Class not found.");

        @class.Name = request.Name;
        @class.Description = request.Description;
        @class.StartDate = request.StartDate;
        @class.EndDate = request.EndDate;
        @class.SkillTags = request.SkillTags;
        @class.TrainerId = request.TrainerId;
        await _db.SaveChangesAsync();
        return ServiceResult<ClassDto>.Success(MapDto(@class));
    }

    public async Task<ServiceResult> ArchiveClassAsync(Guid id)
    {
        var @class = await _db.Classes.FindAsync(id);
        if (@class == null) return ServiceResult.Failure("Class not found.");
        @class.IsArchived = true;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteClassAsync(Guid id)
    {
        var @class = await _db.Classes.FindAsync(id);
        if (@class == null) return ServiceResult.Failure("Class not found.");
        _db.Classes.Remove(@class);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<IEnumerable<UserListItem>> GetEnrolledEmployeesAsync(Guid classId)
    {
        return await _db.ClassEnrollments
            .Include(e => e.Employee)
            .Where(e => e.ClassId == classId && e.Status == EnrollmentStatus.Active)
            .Select(e => new UserListItem(e.Employee.Id, e.Employee.Name, e.Employee.Email, e.Employee.Role, e.Employee.IsActive, null, null))
            .ToListAsync();
    }

    public async Task<ServiceResult> EnrollEmployeeAsync(Guid classId, Guid employeeId)
    {
        if (await _db.ClassEnrollments.AnyAsync(e => e.EmployeeId == employeeId && e.Status == EnrollmentStatus.Active))
            return ServiceResult.Failure("Employee is already enrolled in a class. Transfer them instead.");

        _db.ClassEnrollments.Add(new ClassEnrollment { EmployeeId = employeeId, ClassId = classId });
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> TransferEmployeeAsync(Guid employeeId, Guid targetClassId)
    {
        await _db.ClassEnrollments
            .Where(e => e.EmployeeId == employeeId && e.Status == EnrollmentStatus.Active)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, EnrollmentStatus.Transferred));

        _db.ClassEnrollments.Add(new ClassEnrollment { EmployeeId = employeeId, ClassId = targetClassId });
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveEmployeeAsync(Guid classId, Guid employeeId)
    {
        var enrollment = await _db.ClassEnrollments.FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.ClassId == classId && e.Status == EnrollmentStatus.Active);
        if (enrollment == null) return ServiceResult.Failure("Enrollment not found.");
        enrollment.Status = EnrollmentStatus.Removed;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<ClassDto>> GetMyClassAsync(Guid employeeId)
    {
        var enrollment = await _db.ClassEnrollments
            .Include(e => e.Class).ThenInclude(c => c.Trainer)
            .Include(e => e.Class).ThenInclude(c => c.Enrollments)
            .Include(e => e.Class).ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.Status == EnrollmentStatus.Active);

        if (enrollment == null) return ServiceResult<ClassDto>.Failure("Not enrolled in any class.");
        return ServiceResult<ClassDto>.Success(MapDto(enrollment.Class));
    }

    private static ClassDto MapDto(Class c) => new(
        c.Id, c.Name, c.Description, c.StartDate, c.EndDate, c.SkillTags,
        c.TrainerId, c.Trainer?.Name ?? "", c.IsArchived,
        c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active),
        c.Tasks.Count, c.CreatedAt);
}
