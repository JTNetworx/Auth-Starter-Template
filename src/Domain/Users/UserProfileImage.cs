namespace Domain.Users;

/// <summary>
/// Stores a user's profile image as binary data in the database.
/// Only used when ProfileImageStorage is set to "Database".
/// </summary>
public class UserProfileImage
{
    public string UserId { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = "image/jpeg";
    public DateTime UpdatedAtUtc { get; set; }

    public User? User { get; set; }
}
