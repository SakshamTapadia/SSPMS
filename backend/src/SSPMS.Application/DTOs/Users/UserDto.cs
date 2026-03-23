using SSPMS.Domain.Enums;

namespace SSPMS.Application.DTOs.Users;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    string? AvatarUrl,
    bool IsActive,
    bool TwoFAEnabled,
    DateTime CreatedAt
);

public record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    UserRole Role,
    Guid? ClassId = null
);

public record UpdateUserRequest(
    string Name,
    string? AvatarUrl
);

public record ChangeRoleRequest(UserRole Role);

public record UserListItem(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime? LastLogin,
    string? ClassName
);
