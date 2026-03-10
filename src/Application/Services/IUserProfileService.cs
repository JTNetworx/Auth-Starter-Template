using Application.DTOs.User;
using SharedKernel;

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
}
