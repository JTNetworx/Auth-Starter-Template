using Application.DTOs.User;
using SharedKernel;
// AccountExportDto lives in Application.DTOs.User — same namespace, no extra using needed

namespace Application.Services;

public interface IUserProfileService
{
    Task<Result<UserProfileDto>> GetProfileAsync(string userId);
    Task<Result<UserProfileDto>> UpdateProfileAsync(string userId, UpdateUserProfileDto dto);
    Task<Result<string>> UpdateProfileImageAsync(string userId, Stream imageStream, string contentType);
    Task<Result> DeleteProfileImageAsync(string userId);

    /// <summary>
    /// Returns raw image bytes for database-backed storage to serve via the API.
    /// Returns null when using S3 storage (images are served from CDN directly).
    /// </summary>
    Task<(byte[] Data, string ContentType)?> GetProfileImageBytesAsync(string userId);

    /// <summary>
    /// Hard-deletes the user account and all associated personal data (tokens, passkeys, profile image).
    /// Audit log entries are anonymised automatically via DB cascade (UserId → NULL).
    /// </summary>
    Task<Result> DeleteAccountAsync(string userId);

    /// <summary>
    /// Assembles a full export of the user's personal data: profile, sessions, passkeys, audit history.
    /// </summary>
    Task<Result<AccountExportDto>> ExportDataAsync(string userId);
}
