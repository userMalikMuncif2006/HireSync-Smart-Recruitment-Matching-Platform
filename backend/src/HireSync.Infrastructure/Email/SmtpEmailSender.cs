using HireSync.Application.Interfaces.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HireSync.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailSettings _settings;

    public SmtpEmailSender(SmtpEmailSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            throw new InvalidOperationException(
                "SMTP host is not configured.");
        }

        if (settings.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "SMTP port is invalid.");
        }

        if (string.IsNullOrWhiteSpace(settings.Username))
        {
            throw new InvalidOperationException(
                "SMTP username is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException(
                "SMTP password is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            throw new InvalidOperationException(
                "SMTP from email is not configured.");
        }

        _settings = settings;
    }

    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!MailboxAddress.TryParse(recipientEmail, out var recipient))
        {
            throw new ArgumentException(
                "Recipient email is invalid.",
                nameof(recipientEmail));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException(
                "Email subject is required.",
                nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException(
                "Email body is required.",
                nameof(body));
        }

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _settings.FromName,
                _settings.FromEmail));

        message.To.Add(recipient);

        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            SecureSocketOptions.StartTls,
            cancellationToken);

        await client.AuthenticateAsync(
            _settings.Username,
            _settings.Password,
            cancellationToken);

        await client.SendAsync(
            message,
            cancellationToken);

        await client.DisconnectAsync(
            true,
            cancellationToken);
    }
}
