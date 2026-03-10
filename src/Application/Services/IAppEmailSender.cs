namespace Application.Services;

/// <summary>
/// High-level email sender for application-specific emails.
/// Handles template rendering and link building.
/// Inject this into services that need to send emails.
/// </summary>
public interface IAppEmailSender
{
    /// <summary>Sends an email confirmation link to a newly registered user.</summary>
    Task SendEmailConfirmationAsync(string toEmail, string userName, string userId, string encodedToken);

    /// <summary>Sends a password reset code to the user.</summary>
    Task SendPasswordResetAsync(string toEmail, string userName, string encodedToken);

    /// <summary>Sends a generic alert/notification email.</summary>
    Task SendAlertAsync(string toEmail, string subject, string title, string body);

    /// <summary>
    /// Sends a fully templated email.
    /// Template files live in Infrastructure/EmailTemplates/{templateName}.html
    /// with {{Key}} placeholders.
    /// </summary>
    Task SendTemplatedEmailAsync(string toEmail, string subject, string templateName, Dictionary<string, string> variables);
}
