namespace HireSync.Application.DTOs.Admin;

public sealed record EmployerVerificationUpdateResult(
    bool Succeeded,
    EmployerVerificationSummaryDto? Employer,
    EmployerVerificationUpdateFailureReason? FailureReason)
{
    public static EmployerVerificationUpdateResult Success(
        EmployerVerificationSummaryDto employer)
    {
        return new EmployerVerificationUpdateResult(
            true,
            employer,
            null);
    }

    public static EmployerVerificationUpdateResult Failure(
        EmployerVerificationUpdateFailureReason reason)
    {
        return new EmployerVerificationUpdateResult(
            false,
            null,
            reason);
    }
}
