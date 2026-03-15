using Application.DTOs.Admin;
using SharedKernel;

namespace Application.Services;

public interface IAuditLogService
{
    /// <summary>
    /// Records an audit event. Never throws — failures are logged and swallowed
    /// so audit logging never breaks the main application flow.
    /// </summary>
    Task RecordAsync(string? userId, string action,
        string? entityType = null, string? entityId = null, object? details = null);

    Task<PaginatedResultWithStatus<AuditLogDto>> GetLogsAsync(
        int page, int pageSize,
        string? userId = null, string? action = null,
        DateTime? from = null, DateTime? to = null);
}
