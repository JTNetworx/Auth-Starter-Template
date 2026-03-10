namespace Infrastructure.Options;

/// <summary>
/// Settings for S3-compatible object storage (e.g. Cloudflare R2, AWS S3).
/// Used when App:ProfileImageStorage = "S3".
/// </summary>
public sealed class S3Settings
{
    public const string SectionName = "S3";

    /// <summary>
    /// S3 service URL. For Cloudflare R2: https://&lt;account-id&gt;.r2.cloudflarestorage.com
    /// Leave empty for standard AWS S3.
    /// </summary>
    public string ServiceUrl { get; init; } = string.Empty;

    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public string Region { get; init; } = "auto";

    /// <summary>
    /// Public base URL for serving objects (CDN or R2 public bucket URL).
    /// e.g. https://cdn.yourdomain.com or https://pub-xxx.r2.dev/bucket-name
    /// </summary>
    public string PublicBaseUrl { get; init; } = string.Empty;
}
