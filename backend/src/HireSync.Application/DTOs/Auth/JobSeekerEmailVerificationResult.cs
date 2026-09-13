namespace HireSync.Application.DTOs.Auth;

public enum JobSeekerEmailVerificationFailureReason
{
    InvalidRequest = 1,
    InvalidAccount = 2,
    Suspended = 3,
    AlreadyVerified = 4,
    InvalidCode = 5,
    Expired = 6,
    AlreadyUsed = 7,
    AttemptsExceeded = 8,
    PersistenceFailed = 9
}

public sealed record JobSeekerEmailVerificationResult
{
    private JobSeekerEmailVerificationResult(
        bool succeeded,
        JobSeekerEmailVerificationFailureReason? failureReason)
    {
        Succeeded = succeeded;
        FailureReason = failureReason;
    }

    public bool Succeeded { get; }

    public JobSeekerEmailVerificationFailureReason? FailureReason { get; }

    public static JobSeekerEmailVerificationResult Success() =>
        new(true, null);

    public static JobSeekerEmailVerificationResult Failure(
        JobSeekerEmailVerificationFailureReason reason) =>
        new(false, reason);
}
