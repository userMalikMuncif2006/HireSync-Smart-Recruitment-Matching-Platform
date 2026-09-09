namespace HireSync.Application.DTOs.Auth;

public enum AdministratorActivationRequestFailureReason : byte
{
    InvalidCredentials = 1,
    Suspended = 2,
    AlreadyActivated = 3,
    CooldownActive = 4,
    OtpRequestFailed = 5
}

public sealed record AdministratorActivationRequestResult(
    bool Succeeded,
    DateTime? ExpiresAtUtc,
    AdministratorActivationRequestFailureReason? FailureReason,
    int? RetryAfterSeconds)
{
    public static AdministratorActivationRequestResult Success(
        DateTime expiresAtUtc) =>
        new(true, expiresAtUtc, null, null);

    public static AdministratorActivationRequestResult Failure(
        AdministratorActivationRequestFailureReason reason) =>
        new(false, null, reason, null);

    public static AdministratorActivationRequestResult Cooldown(
        int retryAfterSeconds) =>
        new(
            false,
            null,
            AdministratorActivationRequestFailureReason.CooldownActive,
            retryAfterSeconds);
}
