using Application.DTOs.Auth;

namespace Application.DTOs.Admin;

public record AdminUserSummaryDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    bool EmailConfirmed,
    List<string> Roles,
    DateTime CreatedAtUtc,
    DateTime? LastLoginUtc,
    bool IsLockedOut
);

public record AdminUserDetailDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    bool EmailConfirmed,
    string? PhoneNumber,
    List<string> Roles,
    DateTime CreatedAtUtc,
    DateTime? LastLoginUtc,
    bool IsLockedOut,
    List<SessionDto> ActiveSessions
);

public record AdminAssignRoleDto(string Role);
