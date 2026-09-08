namespace HireSync.Infrastructure.Email;

public sealed class SmtpEmailSettings
{
    public required string Host { get; init; }

    public int Port { get; init; }

    public required string Username { get; init; }

    public required string Password { get; init; }

    public required string FromEmail { get; init; }

    public string FromName { get; init; } = "HireSync";
}
