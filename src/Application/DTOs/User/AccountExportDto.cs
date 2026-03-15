namespace Application.DTOs.User;

public record AccountExportDto(
    ProfileExport Profile,
    List<SessionExport> Sessions,
    List<PasskeyExport> Passkeys,
    List<AuditLogExport> AuditHistory,
    DateTimeOffset ExportedAtUtc);

public record ProfileExport(
    string Id,
    string UserName,
    string Email,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? Street,
    string? Street2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    DateTime CreatedAtUtc,
    DateTime? LastLoginUtc);

public record SessionExport(
    Guid Id,
    DateTime CreatedAtUtc,
    string? IpAddress,
    string? UserAgent,
    DateTime? LastUsedUtc);

public record PasskeyExport(
    string Name,
    DateTime CreatedAt);

public record AuditLogExport(
    string Action,
    string? EntityType,
    DateTime Timestamp,
    string? IpAddress,
    string? Details);
