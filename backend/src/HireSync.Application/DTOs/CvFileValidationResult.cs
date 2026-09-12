namespace HireSync.Application.DTOs;

public sealed record CvFileValidationResult(
    bool Succeeded,
    string? SafeOriginalFileName,
    string? Extension,
    string? ContentType,
    long SizeBytes,
    CvFileValidationFailureReason FailureReason)
{
    public static CvFileValidationResult Success(
        string safeOriginalFileName,
        string extension,
        string contentType,
        long sizeBytes)
    {
        return new CvFileValidationResult(
            true,
            safeOriginalFileName,
            extension,
            contentType,
            sizeBytes,
            CvFileValidationFailureReason.None);
    }

    public static CvFileValidationResult Failure(
        CvFileValidationFailureReason failureReason,
        long sizeBytes = 0)
    {
        return new CvFileValidationResult(
            false,
            null,
            null,
            null,
            sizeBytes,
            failureReason);
    }
}
