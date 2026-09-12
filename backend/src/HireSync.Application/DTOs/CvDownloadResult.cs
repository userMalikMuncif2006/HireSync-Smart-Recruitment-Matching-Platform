namespace HireSync.Application.DTOs;

public sealed record CvDownloadResult(
    bool Succeeded,
    Stream? Content,
    string? OriginalFileName,
    string? ContentType,
    CvDownloadFailureReason FailureReason)
{
    public static CvDownloadResult Success(
        Stream content,
        string originalFileName,
        string contentType)
    {
        ArgumentNullException.ThrowIfNull(
            content);

        return new CvDownloadResult(
            true,
            content,
            originalFileName,
            contentType,
            CvDownloadFailureReason.None);
    }

    public static CvDownloadResult Failure(
        CvDownloadFailureReason failureReason)
    {
        return new CvDownloadResult(
            false,
            null,
            null,
            null,
            failureReason);
    }
}
