using System.ComponentModel.DataAnnotations;

namespace WebClient.Services;

// ── Auth Request/Response Models ─────────────────────────────────────────────

public record TokenDto(string AccessToken, string RefreshToken);

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
