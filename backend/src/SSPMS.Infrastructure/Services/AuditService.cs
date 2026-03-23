using Microsoft.EntityFrameworkCore;
using SSPMS.Application.Common;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Entities;
using SSPMS.Infrastructure.Data;

namespace SSPMS.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(Guid? userId, string action, string? entity = null, Guid? entityId = null, string ipAddress = "", string? userAgent = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLogItem>> GetLogsAsync(int page, int pageSize, Guid? userId = null, string? action = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (userId.HasValue) query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action.Contains(action));
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogItem(a.Id, a.UserId, a.User != null ? a.User.Name : null, a.Action, a.Entity, a.EntityId, a.IPAddress, a.UserAgent, a.Timestamp))
            .ToListAsync();

        return new PagedResult<AuditLogItem> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}
