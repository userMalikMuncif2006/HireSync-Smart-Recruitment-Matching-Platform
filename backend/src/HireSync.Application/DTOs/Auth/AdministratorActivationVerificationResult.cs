namespace HireSync.Application.DTOs.Auth;

public enum AdministratorActivationVerificationFailureReason : byte
{
    InvalidCredentials = 1,
    Suspended = 2,
    AlreadyActivated = 3,
    InvalidCode = 4,
    Expired = 5,
    AlreadyUsed = 6,
    AttemptsExceeded = 7,
    PersistenceFailed = 8
}

public sealed record AdministratorActivationVerificationResult(
    bool Succeeded,
    AdministratorActivationVerificationFailureReason? FailureReason)
{
    public static AdministratorActivationVerificationResult Success() =>
        new(true, null);

    public static AdministratorActivationVerificationResult Failure(
        AdministratorActivationVerificationFailureReason reason) =>
        new(false, reason);
}
