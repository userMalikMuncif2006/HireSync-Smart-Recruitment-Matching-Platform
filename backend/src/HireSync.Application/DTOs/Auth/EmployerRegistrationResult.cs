namespace HireSync.Application.DTOs.Auth;

public enum EmployerRegistrationFailureReason
{
    InvalidInput = 1,
    EmailAlreadyExists = 2,
    DuplicateBusinessRegistrationNumber = 3,
    IdentityValidationFailed = 4,
    PersistenceFailed = 5
}

public sealed record EmployerRegistrationResult
{
    private EmployerRegistrationResult(
        bool succeeded,
        RegisterEmployerResponse? response,
        EmployerRegistrationFailureReason? failureReason)
    {
        Succeeded = succeeded;
        Response = response;
        FailureReason = failureReason;
    }

    public bool Succeeded { get; }

    public RegisterEmployerResponse? Response { get; }

    public EmployerRegistrationFailureReason? FailureReason { get; }

    public static EmployerRegistrationResult Success(
        RegisterEmployerResponse response) =>
        new(true, response, null);

    public static EmployerRegistrationResult Failure(
        EmployerRegistrationFailureReason reason) =>
        new(false, null, reason);
}
