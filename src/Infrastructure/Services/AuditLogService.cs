using Application.DTOs.Admin;
using Application.Services;
using Domain.Audit;
using Infrastructure.Persistance;
using Infrastructure.Persistance.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharedKernel;
using System.Text.Json;

namespace Infrastructure.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repo;
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IAuditLogRepository repo,
        ApplicationDbContext db,
        IHttpContextAccessor http,
        IDateTimeProvider dateTime,
        ILogger<AuditLogService> logger)
    {
        _repo = repo;
        _db = db;
        _http = http;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task RecordAsync(string? userId, string action,
        string? entityType = null, string? entityId = null, object? details = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = _http.HttpContext?.Request.Headers.UserAgent.ToString(),
                Timestamp = _dateTime.UtcNow,
                Details = details is null ? null : JsonSerializer.Serialize(details)
            };

            _repo.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Audit logging must never break the main flow.
            _logger.LogWarning(ex, "Failed to write audit log for action {Action} userId {UserId}", action, userId);
        }
    }

    public async Task<PaginatedResultWithStatus<AuditLogDto>> GetLogsAsync(
        int page, int pageSize,
        string? userId = null, string? action = null,
        DateTime? from = null, DateTime? to = null)
    {
        var (items, totalCount) = await _repo.GetPagedAsync(page, pageSize, userId, action, from, to);

        var dtos = items.Select(l => new AuditLogDto(
            l.Id,
            l.UserId,
            l.User is null ? null : $"{l.User.FirstName} {l.User.LastName}".Trim(),
            l.User?.UserName,
            l.Action,
            l.EntityType,
            l.EntityId,
            l.IpAddress,
            l.UserAgent,
            l.Timestamp,
            l.Details
        )).ToList();

        return PaginatedResultWithStatus<AuditLogDto>.Success(dtos, page, pageSize, totalCount);
    }
}
