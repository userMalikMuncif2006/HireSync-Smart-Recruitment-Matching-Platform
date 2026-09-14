namespace HireSync.Application.DTOs;

public sealed record JobSeekerProfileUpdateResult(
    bool Succeeded,
    JobSeekerProfileDto? Profile,
    JobSeekerProfileUpdateFailureReason FailureReason)
{
    public static JobSeekerProfileUpdateResult Success(
        JobSeekerProfileDto profile) =>
        new(
            true,
            profile,
            JobSeekerProfileUpdateFailureReason.None);

    public static JobSeekerProfileUpdateResult Failure(
        JobSeekerProfileUpdateFailureReason failureReason) =>
        new(
            false,
            null,
            failureReason);
}