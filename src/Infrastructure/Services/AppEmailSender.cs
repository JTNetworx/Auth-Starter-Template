using Application.Services;
using Infrastructure.Email;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// High-level application email sender.
/// Renders HTML templates and delegates transport to IEmailSender (SMTP via MailKit).
/// Inject this wherever the application needs to send emails (AuthService, etc.)
/// </summary>
public sealed class AppEmailSender : IAppEmailSender
{
    private readonly IEmailSender _transport;
    private readonly AppSettings _app;

    public AppEmailSender(IEmailSender transport, IOptions<AppSettings> app)
    {
        _transport = transport;
        _app = app.Value;
    }

    public Task SendEmailConfirmationAsync(string toEmail, string userName, string userId, string encodedToken)
    {
        var confirmLink = $"{_app.ClientBaseUrl}/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(encodedToken)}";

        var html = EmailTemplateRenderer.Render("ConfirmEmail", new Dictionary<string, string>
        {
            ["AppName"]     = _app.Name,
            ["UserName"]    = userName,
            ["ConfirmLink"] = confirmLink,
            ["Year"]        = DateTime.UtcNow.Year.ToString()
        });

        return _transport.SendAsync(toEmail, $"Confirm your email – {_app.Name}", html);
    }

    public Task SendPasswordResetAsync(string toEmail, string userName, string encodedToken)
    {
        var resetLink = $"{_app.ClientBaseUrl}/reset-password?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(encodedToken)}";

        var html = EmailTemplateRenderer.Render("PasswordReset", new Dictionary<string, string>
        {
            ["AppName"]   = _app.Name,
            ["UserName"]  = userName,
            ["ResetLink"] = resetLink,
            ["Year"]      = DateTime.UtcNow.Year.ToString()
        });

        return _transport.SendAsync(toEmail, $"Reset your password – {_app.Name}", html);
    }

    public Task SendAlertAsync(string toEmail, string subject, string title, string body)
    {
        var html = EmailTemplateRenderer.Render("Alert", new Dictionary<string, string>
        {
            ["AppName"] = _app.Name,
            ["Title"]   = title,
            ["Body"]    = body,
            ["Year"]    = DateTime.UtcNow.Year.ToString()
        });

        return _transport.SendAsync(toEmail, subject, html);
    }

    public Task SendTemplatedEmailAsync(string toEmail, string subject, string templateName, Dictionary<string, string> variables)
    {
        // Inject common variables if not already provided
        variables.TryAdd("AppName", _app.Name);
        variables.TryAdd("Year", DateTime.UtcNow.Year.ToString());

        var html = EmailTemplateRenderer.Render(templateName, variables);
        return _transport.SendAsync(toEmail, subject, html);
    }
}
