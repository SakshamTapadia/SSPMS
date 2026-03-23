using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Classes;
using SSPMS.Application.DTOs.Users;

namespace SSPMS.Application.Interfaces;

public interface IClassService
{
    Task<IEnumerable<ClassDto>> GetClassesAsync(Guid? trainerId = null);
    Task<ServiceResult<ClassDto>> GetByIdAsync(Guid id, Guid requesterId, string role);
    Task<ServiceResult<ClassDto>> CreateClassAsync(CreateClassRequest request);
    Task<ServiceResult<ClassDto>> UpdateClassAsync(Guid id, UpdateClassRequest request);
    Task<ServiceResult> ArchiveClassAsync(Guid id);
    Task<ServiceResult> DeleteClassAsync(Guid id);
    Task<IEnumerable<UserListItem>> GetEnrolledEmployeesAsync(Guid classId);
    Task<ServiceResult> EnrollEmployeeAsync(Guid classId, Guid employeeId);
    Task<ServiceResult> TransferEmployeeAsync(Guid employeeId, Guid targetClassId);
    Task<ServiceResult> RemoveEmployeeAsync(Guid classId, Guid employeeId);
    Task<ServiceResult<ClassDto>> GetMyClassAsync(Guid employeeId);
}
