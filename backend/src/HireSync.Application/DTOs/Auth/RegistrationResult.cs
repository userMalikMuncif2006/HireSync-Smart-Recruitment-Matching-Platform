namespace HireSync.Application.DTOs.Auth;

public enum RegistrationFailureReason
{
    InvalidInput = 1,
    EmailAlreadyExists = 2,
    IdentityValidationFailed = 3
}

public sealed record RegistrationResult
{
    private RegistrationResult(
        bool succeeded,
        RegisterJobSeekerResponse? response,
        RegistrationFailureReason? failureReason)
    {
        Succeeded = succeeded;
        Response = response;
        FailureReason = failureReason;
    }

    public bool Succeeded { get; }

    public RegisterJobSeekerResponse? Response { get; }

    public RegistrationFailureReason? FailureReason { get; }

    public static RegistrationResult Success(
        RegisterJobSeekerResponse response) =>
        new(true, response, null);

    public static RegistrationResult Failure(
        RegistrationFailureReason reason) =>
        new(false, null, reason);
}
