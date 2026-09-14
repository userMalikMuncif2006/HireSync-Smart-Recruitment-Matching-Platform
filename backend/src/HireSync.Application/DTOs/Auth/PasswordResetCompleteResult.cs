namespace HireSync.Application.DTOs.Auth;

public enum PasswordResetCompleteFailureReason
{
    InvalidRequest = 1,
    InvalidOrExpiredCode = 2,
    InvalidPassword = 3,
    PersistenceFailed = 4
}

public sealed record PasswordResetCompleteResult
{
    private PasswordResetCompleteResult(
        bool succeeded,
        PasswordResetCompleteFailureReason? failureReason,
        IReadOnlyList<string> passwordErrors)
    {
        Succeeded = succeeded;
        FailureReason = failureReason;
        PasswordErrors = passwordErrors;
    }

    public bool Succeeded { get; }

    public PasswordResetCompleteFailureReason? FailureReason { get; }

    public IReadOnlyList<string> PasswordErrors { get; }

    public static PasswordResetCompleteResult Success() =>
        new(
            true,
            null,
            Array.Empty<string>());

    public static PasswordResetCompleteResult Failure(
        PasswordResetCompleteFailureReason reason) =>
        new(
            false,
            reason,
            Array.Empty<string>());

    public static PasswordResetCompleteResult InvalidPassword(
        IReadOnlyList<string> errors) =>
        new(
            false,
            PasswordResetCompleteFailureReason.InvalidPassword,
            errors);
}
