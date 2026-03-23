using SSPMS.Application.Common;

namespace SSPMS.Application.Interfaces;

public record AuditLogItem(
    Guid Id,
    Guid? UserId,
    string? UserName,
    string Action,
    string? Entity,
    Guid? EntityId,
    string IPAddress,
    string? UserAgent,
    DateTime Timestamp
);

public interface IAuditService
{
    Task LogAsync(Guid? userId, string action, string? entity = null, Guid? entityId = null, string ipAddress = "", string? userAgent = null);
    Task<PagedResult<AuditLogItem>> GetLogsAsync(int page, int pageSize, Guid? userId = null, string? action = null, DateTime? from = null, DateTime? to = null);
}
