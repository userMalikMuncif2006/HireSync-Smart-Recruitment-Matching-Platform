namespace HireSync.Application.DTOs;

public enum CvDownloadFailureReason
{
    None = 0,
    InvalidAuthenticatedUser = 1,
    NotFound = 2,
    StorageUnavailable = 3
}
