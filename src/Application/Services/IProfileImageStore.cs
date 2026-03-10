namespace Application.Services;

/// <summary>
/// Abstraction over profile image storage.
/// Implementations: DatabaseProfileImageStore, S3ProfileImageStore.
/// Configured via App:ProfileImageStorage ("Database" | "S3").
/// </summary>
public interface IProfileImageStore
{
    /// <summary>
    /// Saves the image and returns the full URL to store on the user record.
    /// </summary>
    Task<string> SaveAsync(string userId, Stream imageStream, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the raw image data. Returns null if not found.
    /// Only used by database storage to serve the image via the API.
    /// </summary>
    Task<(byte[] Data, string ContentType)?> GetAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes the stored image for the given user.
    /// </summary>
    Task DeleteAsync(string userId, CancellationToken ct = default);
}
