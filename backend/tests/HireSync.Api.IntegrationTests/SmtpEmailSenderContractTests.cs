using HireSync.Infrastructure.Email;

namespace HireSync.Api.IntegrationTests;

public class SmtpEmailSenderContractTests
{
    [Fact]
    public void Missing_smtp_host_is_rejected()
    {
        var settings = new SmtpEmailSettings
        {
            Host = "",
            Port = 587,
            Username = "sender@example.com",
            Password = "secret",
            FromEmail = "sender@example.com"
        };

        Assert.Throws<InvalidOperationException>(() =>
            new SmtpEmailSender(settings));
    }

    [Fact]
    public void Invalid_smtp_port_is_rejected()
    {
        var settings = new SmtpEmailSettings
        {
            Host = "smtp.example.com",
            Port = 0,
            Username = "sender@example.com",
            Password = "secret",
            FromEmail = "sender@example.com"
        };

        Assert.Throws<InvalidOperationException>(() =>
            new SmtpEmailSender(settings));
    }
}
