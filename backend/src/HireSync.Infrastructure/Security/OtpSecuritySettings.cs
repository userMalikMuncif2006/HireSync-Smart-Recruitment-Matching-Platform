namespace HireSync.Infrastructure.Security;

public sealed class OtpSecuritySettings
{
    public required string HashingKey { get; init; }
}
