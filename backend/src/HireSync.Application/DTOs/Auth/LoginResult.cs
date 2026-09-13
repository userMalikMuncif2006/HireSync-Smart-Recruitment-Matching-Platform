namespace HireSync.Application.DTOs.Auth;

public enum LoginFailureReason
{
    InvalidCredentials = 1,
    Suspended = 2,
    AdministratorActivationRequired = 3,
    EmployerEmailVerificationRequired = 4,
    JobSeekerEmailVerificationRequired = 5
}

public sealed record LoginResult
{
    private LoginResult(
        bool succeeded,
        LoginResponse? response,
        LoginFailureReason? failureReason)
    {
        Succeeded = succeeded;
        Response = response;
        FailureReason = failureReason;
    }

    public bool Succeeded { get; }

    public LoginResponse? Response { get; }

    public LoginFailureReason? FailureReason { get; }

    public static LoginResult Success(LoginResponse response) =>
        new(true, response, null);

    public static LoginResult Failure(LoginFailureReason reason) =>
        new(false, null, reason);
}
