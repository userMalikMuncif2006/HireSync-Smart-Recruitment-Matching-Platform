namespace HireSync.Application.DTOs;

public enum CvOperationFailureReason
{
    None = 0,
    InvalidAuthenticatedUser = 1,
    ProfileNotFound = 2,
    InvalidInput = 3,
    InvalidFileType = 4,
    FileTooLarge = 5,
    MalformedFile = 6,
    StorageUnavailable = 7,
    PersistenceFailed = 8
}
