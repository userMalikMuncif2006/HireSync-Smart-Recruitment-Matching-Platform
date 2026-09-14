namespace HireSync.Application.DTOs.Auth;

public enum EmployerEmailVerificationRequestFailureReason
{
    InvalidRequest = 1,
    InvalidAccount = 2,
    Suspended = 3,
    AlreadyVerified = 4,
    CooldownActive = 5,
    RequestFailed = 6
}

public sealed record EmployerEmailVerificationRequestResult
{
    private EmployerEmailVerificationRequestResult(
        bool succeeded,
        DateTime? expiresAtUtc,
        int? retryAfterSeconds,
        EmployerEmailVerificationRequestFailureReason? failureReason)
    {
        Succeeded = succeeded;
        ExpiresAtUtc = expiresAtUtc;
        RetryAfterSeconds = retryAfterSeconds;
        FailureReason = failureReason;
    }

    public bool Succeeded { get; }

    public DateTime? ExpiresAtUtc { get; }

    public int? RetryAfterSeconds { get; }

    public EmployerEmailVerificationRequestFailureReason? FailureReason { get; }

    public static EmployerEmailVerificationRequestResult Success(
        DateTime expiresAtUtc) =>
        new(
            true,
            expiresAtUtc,
            null,
            null);

    public static EmployerEmailVerificationRequestResult Cooldown(
        int retryAfterSeconds) =>
        new(
            false,
            null,
            retryAfterSeconds,
            EmployerEmailVerificationRequestFailureReason.CooldownActive);

    public static EmployerEmailVerificationRequestResult Failure(
        EmployerEmailVerificationRequestFailureReason reason) =>
        new(
            false,
            null,
            null,
            reason);
}
