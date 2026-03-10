namespace Infrastructure.Options;

public sealed class AppSettings
{
    public const string SectionName = "App";

    public string Name { get; init; } = "App";

    /// <summary>
    /// Base URL of the frontend client. Used to build links in emails.
    /// e.g. https://localhost:5001
    /// </summary>
    public string ClientBaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Base URL of this API. Used to build profile image URLs for database storage.
    /// e.g. https://localhost:7170
    /// </summary>
    public string ApiBaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Profile image storage strategy. Values: "Database" | "S3". Default: "Database".
    /// </summary>
    public string ProfileImageStorage { get; init; } = "Database";
}
