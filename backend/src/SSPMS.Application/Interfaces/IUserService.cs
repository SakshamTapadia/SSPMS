using SSPMS.Application.Common;
using SSPMS.Application.DTOs.Users;
using SSPMS.Domain.Enums;

namespace SSPMS.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserListItem>> GetUsersAsync(int page, int pageSize, string? search, UserRole? role, bool? isActive);
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request, Guid createdBy);
    Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<ServiceResult> ChangeRoleAsync(Guid id, UserRole newRole);
    Task<ServiceResult> DeactivateUserAsync(Guid id);
    Task<ServiceResult> ReactivateUserAsync(Guid id);
    Task<ServiceResult> UpdateAvatarAsync(Guid id, string avatarUrl);
    Task<ServiceResult<(int success, IEnumerable<string> errors)>> BulkImportAsync(Stream csv, Guid classId, Guid createdBy);
    Task<ServiceResult<UserDto>> CreateEmployeeByTrainerAsync(CreateUserRequest request, Guid trainerId);
}
