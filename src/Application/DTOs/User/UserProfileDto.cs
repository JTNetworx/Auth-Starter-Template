namespace Application.DTOs.User;

public record UserProfileDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? PhoneNumber { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Street { get; init; }
    public string? Street2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? ProfileImageUrl { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastLoginUtc { get; init; }
}
