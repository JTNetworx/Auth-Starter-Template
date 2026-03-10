using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.User;

public record UpdateUserProfileDto
{
    [Required][StringLength(100)] public string FirstName { get; init; } = string.Empty;
    [Required][StringLength(100)] public string LastName { get; init; } = string.Empty;
    [Phone][StringLength(20)] public string? PhoneNumber { get; init; }
    public DateTime? DateOfBirth { get; init; }
    [StringLength(200)] public string? Street { get; init; }
    [StringLength(200)] public string? Street2 { get; init; }
    [StringLength(100)] public string? City { get; init; }
    [StringLength(100)] public string? State { get; init; }
    [StringLength(20)] public string? PostalCode { get; init; }
    public int? CountryId { get; init; }
}
