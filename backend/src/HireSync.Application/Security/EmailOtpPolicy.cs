namespace HireSync.Application.Security;

public static class EmailOtpPolicy
{
    public const int CodeLength = 6;

    public static readonly TimeSpan Lifetime =
        TimeSpan.FromMinutes(5);

    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan ResendCooldown =
        TimeSpan.FromSeconds(60);
}
