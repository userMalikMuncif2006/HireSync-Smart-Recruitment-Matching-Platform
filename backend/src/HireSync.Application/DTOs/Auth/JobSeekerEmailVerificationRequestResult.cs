namespace HireSync.Application.DTOs.Auth;

public enum JobSeekerEmailVerificationRequestFailureReason
{
    InvalidRequest = 1,
    InvalidAccount = 2,
    Suspended = 3,
    AlreadyVerified = 4,
    CooldownActive = 5,
    RequestFailed = 6
}

public sealed record JobSeekerEmailVerificationRequestResult
{
    private JobSeekerEmailVerificationRequestResult(
        bool succeeded,
        DateTime? expiresAtUtc,
        int? retryAfterSeconds,
        JobSeekerEmailVerificationRequestFailureReason? failureReason)
    {
        Succeeded = succeeded;
        ExpiresAtUtc = expiresAtUtc;
        RetryAfterSeconds = retryAfterSeconds;
        FailureReason = failureReason;
    }

    public bool Succeeded { get; }

    public DateTime? ExpiresAtUtc { get; }

    public int? RetryAfterSeconds { get; }

    public JobSeekerEmailVerificationRequestFailureReason? FailureReason { get; }

    public static JobSeekerEmailVerificationRequestResult Success(
        DateTime expiresAtUtc) =>
        new(
            true,
            expiresAtUtc,
            null,
            null);

    public static JobSeekerEmailVerificationRequestResult Cooldown(
        int retryAfterSeconds) =>
        new(
            false,
            null,
            retryAfterSeconds,
            JobSeekerEmailVerificationRequestFailureReason.CooldownActive);

    public static JobSeekerEmailVerificationRequestResult Failure(
        JobSeekerEmailVerificationRequestFailureReason reason) =>
        new(
            false,
            null,
            null,
            reason);
}
