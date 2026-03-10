namespace Domain.Users;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public bool IsActive => RevokedAtUtc == null && !IsExpired;

    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; } = null!;
}
