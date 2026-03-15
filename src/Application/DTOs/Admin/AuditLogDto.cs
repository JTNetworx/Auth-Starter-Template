namespace Application.DTOs.Admin;

public record AuditLogDto(
    long Id,
    string? UserId,
    string? UserFullName,
    string? UserName,
    string Action,
    string? EntityType,
    string? EntityId,
    string? IpAddress,
    string? UserAgent,
    DateTime Timestamp,
    string? Details
);
