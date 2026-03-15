using Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Repositories;

public interface IAuditLogRepository
{
    void Add(AuditLog log);
    Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? userId, string? action,
        DateTime? from, DateTime? to);
}

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _db;

    public AuditLogRepository(ApplicationDbContext db) => _db = db;

    public void Add(AuditLog log) => _db.AuditLogs.Add(log);

    public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? userId, string? action,
        DateTime? from, DateTime? to)
    {
        var query = _db.AuditLogs
            .Include(l => l.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(l => l.UserId == userId);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
