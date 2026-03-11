namespace Application.DTOs.Auth;

public record SessionDto(
    Guid Id,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime? LastUsedUtc,
    bool IsCurrent
);
