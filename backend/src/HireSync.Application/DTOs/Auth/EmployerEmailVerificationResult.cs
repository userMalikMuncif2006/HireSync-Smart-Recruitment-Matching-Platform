namespace HireSync.Application.DTOs.Auth;

public enum EmployerEmailVerificationFailureReason
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

public sealed record EmployerEmailVerificationResult
{
    private EmployerEmailVerificationResult(
        bool succeeded,
        EmployerEmailVerificationFailureReason? failureReason)
    {
        Succeeded = succeeded;
        FailureReason = failureReason;
    }

    public bool Succeeded { get; }

    public EmployerEmailVerificationFailureReason? FailureReason { get; }

    public static EmployerEmailVerificationResult Success() =>
        new(true, null);

    public static EmployerEmailVerificationResult Failure(
        EmployerEmailVerificationFailureReason reason) =>
        new(false, reason);
}
