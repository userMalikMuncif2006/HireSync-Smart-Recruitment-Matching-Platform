namespace HireSync.Application.DTOs.Employer;

public enum EmployerProfileUpdateFailureReason
{
    None = 0,
    NotFound = 1,
    InvalidInput = 2,
    DuplicateBusinessRegistrationNumber = 3,
    VerificationResetRequired = 4,
    PersistenceFailed = 5
}