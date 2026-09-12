namespace HireSync.Application.DTOs;

public enum JobSeekerProfileUpdateFailureReason
{
    None = 0,
    InvalidAuthenticatedUser = 1,
    InvalidInput = 2,
    SkillNotFound = 3,
    PersistenceFailed = 4
}