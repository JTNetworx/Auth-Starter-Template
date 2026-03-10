using Microsoft.AspNetCore.Identity;

namespace Domain.Users;

public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; } = null;
    public string? Street { get; set; } = null;
    public string? Street2 { get; set; } = null;
    public string? City { get; set; } = null;
    public string? State { get; set; } = null;
    public string? PostalCode { get; set; } = null;
    public int? CountryId { get; set; } = null;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; } = null;
    public DateTime? LastLoginUtc { get; set; } = null;

    //TODO: Figure out weather to place tokens inside User Table or to have a separate table for tokens and link them to the user via UserId
    //public string? CurrentRefreshToken { get; set; } = string.Empty;
    //public DateTime? CurrentRefreshTokenExpiryUtc { get; set; }
    //public DateTime? CurrentRefreshTokenCreatedAtUtc { get; set; }
    //public string? LastRefreshToken { get; set; } = string.Empty;
    //public DateTime? LastRefreshTokenExpiryUtc { get; set; }
    //public DateTime? LastRefreshTokenCreatedAtUtc { get; set; }

    public string? ProfileImageUrl { get; set; } = null;

    public AppCountry? Country { get; set; } = null;
    public UserProfileImage? ProfileImage { get; set; } = null;
}
