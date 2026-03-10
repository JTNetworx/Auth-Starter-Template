namespace Infrastructure.Options;

public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 465;

    /// <summary>
    /// Only relevant when Port is NOT 465.
    /// Port 465 always uses implicit SSL (SslOnConnect) — this value is ignored.
    /// Port 587: true = require StartTLS, false = use StartTLS when available.
    /// </summary>
    public bool EnableSsl { get; init; } = false;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
}
