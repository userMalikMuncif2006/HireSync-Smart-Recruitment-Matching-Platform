namespace HireSync.Application.DTOs;

public sealed record CvUploadResult(
    bool Succeeded,
    JobSeekerCvDto? Cv,
    CvOperationFailureReason FailureReason)
{
    public static CvUploadResult Success(
        JobSeekerCvDto cv)
    {
        return new CvUploadResult(
            true,
            cv,
            CvOperationFailureReason.None);
    }

    public static CvUploadResult Failure(
        CvOperationFailureReason failureReason)
    {
        return new CvUploadResult(
            false,
            null,
            failureReason);
    }
}
