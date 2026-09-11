namespace HireSync.Application.DTOs.Employer;

public sealed record EmployerProfileUpdateResult(
    EmployerProfileDto? Profile,
    EmployerProfileUpdateFailureReason FailureReason)
{
    public bool Succeeded =>
        FailureReason == EmployerProfileUpdateFailureReason.None &&
        Profile is not null;

    public static EmployerProfileUpdateResult Success(
        EmployerProfileDto profile)
    {
        return new EmployerProfileUpdateResult(
            profile,
            EmployerProfileUpdateFailureReason.None);
    }

    public static EmployerProfileUpdateResult Failure(
        EmployerProfileUpdateFailureReason failureReason)
    {
        return new EmployerProfileUpdateResult(
            null,
            failureReason);
    }
}