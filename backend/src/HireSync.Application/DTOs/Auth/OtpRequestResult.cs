namespace HireSync.Application.DTOs.Auth;

public sealed record OtpRequestResult(
    bool Succeeded,
    DateTime? ExpiresAtUtc,
    OtpRequestFailureReason? FailureReason,
    int? RetryAfterSeconds)
{
    public static OtpRequestResult Success(
        DateTime expiresAtUtc)
    {
        return new(
            true,
            expiresAtUtc,
            null,
            null);
    }

    public static OtpRequestResult Cooldown(
        int retryAfterSeconds)
    {
        return new(
            false,
            null,
            OtpRequestFailureReason.CooldownActive,
            retryAfterSeconds);
    }
}
