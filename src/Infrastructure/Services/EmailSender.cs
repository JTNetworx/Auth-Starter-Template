using Application.Services;
using Domain.Users;
using Infrastructure.Email;
using Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Services;

/// <summary>
/// SMTP email sender using MailKit.
/// Implements:
///   - Application.Services.IEmailSender  (low-level transport, used by AppEmailSender)
///   - Microsoft.AspNetCore.Identity.IEmailSender{User}  (Identity plumbing for built-in endpoints)
/// </summary>
public sealed class EmailSender : IEmailSender, IEmailSender<User>
{
    private readonly SmtpSettings _smtp;
    private readonly AppSettings _app;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IOptions<SmtpSettings> smtp,
        IOptions<AppSettings> app,
        ILogger<EmailSender> logger)
    {
        _smtp = smtp.Value;
        _app = app.Value;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Application.Services.IEmailSender — low-level transport
    // -----------------------------------------------------------------------

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            _logger.LogWarning("[EMAIL SKIPPED] SMTP not configured. To: {To} | Subject: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            // Port 465  → implicit SSL (SslOnConnect) — EnableSsl setting ignored, connection is always encrypted.
            // Port 587  → explicit TLS (STARTTLS).  Set EnableSsl: true to require it, false to allow it when available.
            var socketOptions = _smtp.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : _smtp.EnableSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(_smtp.Host, _smtp.Port, socketOptions);

            if (!string.IsNullOrEmpty(_smtp.Username))
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);

            _logger.LogInformation("Email sent. To: {To} | Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            // Email delivery failures must never fail the calling business operation.
            // Log at ERROR for alerting — the user/admin should investigate SMTP config.
            _logger.LogError(ex, "Failed to send email. To: {To} | Subject: {Subject}", toEmail, subject);
        }
    }

    // -----------------------------------------------------------------------
    // Microsoft.AspNetCore.Identity.IEmailSender<User>
    // Called by Identity's built-in endpoint mapping (MapIdentityApi / Razor Pages UI).
    // Our own controllers use IAppEmailSender instead.
    // -----------------------------------------------------------------------

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        var html = EmailTemplateRenderer.Render("ConfirmEmail", new Dictionary<string, string>
        {
            ["AppName"]     = _app.Name,
            ["UserName"]    = user.UserName ?? email,
            ["ConfirmLink"] = confirmationLink,
            ["Year"]        = DateTime.UtcNow.Year.ToString()
        });

        return SendAsync(email, $"Confirm your email – {_app.Name}", html);
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        var html = EmailTemplateRenderer.Render("PasswordReset", new Dictionary<string, string>
        {
            ["AppName"]   = _app.Name,
            ["UserName"]  = user.UserName ?? email,
            ["ResetLink"] = resetLink,
            ["Year"]      = DateTime.UtcNow.Year.ToString()
        });

        return SendAsync(email, $"Reset your password – {_app.Name}", html);
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        // resetCode from Identity is the raw token; build a reset link for the client.
        var resetLink = $"{_app.ClientBaseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetCode)}";

        var html = EmailTemplateRenderer.Render("PasswordReset", new Dictionary<string, string>
        {
            ["AppName"]   = _app.Name,
            ["UserName"]  = user.UserName ?? email,
            ["ResetLink"] = resetLink,
            ["Year"]      = DateTime.UtcNow.Year.ToString()
        });

        return SendAsync(email, $"Reset your password – {_app.Name}", html);
    }
}
