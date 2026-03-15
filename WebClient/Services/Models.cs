using System.ComponentModel.DataAnnotations;

namespace WebClient.Services;

// ── Auth Request/Response Models ─────────────────────────────────────────────

public record TokenDto(string AccessToken, string RefreshToken);

/// <summary>
/// Returned by POST /api/auth/login.
/// When RequiresTwoFactor is true, call POST /api/auth/2fa/verify with UserId + code.
/// </summary>
public record LoginResult(bool RequiresTwoFactor, string? UserId, TokenDto? Tokens);

public record TwoFactorSetupInfo(string SharedKey, string AuthenticatorUri, string QrCodeBase64);

public class TwoFactorVerifyRequest
{
    public string UserId { get; set; } = string.Empty;
    [Required][StringLength(8, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
}

public class TwoFactorCodeRequest
{
    [Required][StringLength(8, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required] public string UserName { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required][StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required][StringLength(100)] public string LastName { get; set; } = string.Empty;
    [Required][StringLength(256)] public string UserName { get; set; } = string.Empty;
    [Required][EmailAddress][StringLength(256)] public string Email { get; set; } = string.Empty;
    [Required][StringLength(256, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Required][Compare(nameof(Password), ErrorMessage = "Passwords do not match.")] public string ConfirmPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required][EmailAddress] public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required][EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
    [Required][StringLength(256, MinimumLength = 8)] public string NewPassword { get; set; } = string.Empty;
    [Required][Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")] public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required][StringLength(256, MinimumLength = 8)] public string NewPassword { get; set; } = string.Empty;
    [Required][Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")] public string ConfirmNewPassword { get; set; } = string.Empty;
}

// ── User Models ───────────────────────────────────────────────────────────────

public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Street { get; set; }
    public string? Street2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }
}

public class UpdateProfileRequest
{
    [Required][StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required][StringLength(100)] public string LastName { get; set; } = string.Empty;
    [Phone][StringLength(20)] public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [StringLength(200)] public string? Street { get; set; }
    [StringLength(200)] public string? Street2 { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(100)] public string? State { get; set; }
    [StringLength(20)] public string? PostalCode { get; set; }
    public int? CountryId { get; set; }
}

// ── Country ───────────────────────────────────────────────────────────────────

public record CountryDto(int Id, string Name);

// ── Passkey ───────────────────────────────────────────────────────────────────

public record PasskeyInfoDto(string CredentialId, string? Name, DateTimeOffset CreatedAt);

// ── Session ───────────────────────────────────────────────────────────────────

public record SessionDto(
    Guid Id,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime? LastUsedUtc,
    bool IsCurrent
);

// ── Admin ─────────────────────────────────────────────────────────────────────

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
)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

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
)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

// ── Background Jobs ───────────────────────────────────────────────────────────

public record JobStatusDto(
    string Name,
    string? Description,
    string TriggerState,
    DateTimeOffset? NextFireTimeUtc,
    DateTimeOffset? PreviousFireTimeUtc,
    string? CronExpression,
    bool IsRunning,
    DateTimeOffset? LastRunUtc,
    string? LastRunStatus,      // "Success" | "Failed" | null (never run this session)
    string? LastRunDetails,     // e.g. "Deleted 10 refresh token(s)."
    long? LastRunDurationMs,
    string? LastRunTriggeredBy);  // null = cron schedule, userId = manual admin trigger

// ── Audit Log ─────────────────────────────────────────────────────────────────

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

// ── API Result ────────────────────────────────────────────────────────────────

public class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }

    public static ApiResult<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static ApiResult<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

public class ApiResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    public static ApiResult Success() => new() { IsSuccess = true };
    public static ApiResult Failure(string error) => new() { IsSuccess = false, Error = error };
}
