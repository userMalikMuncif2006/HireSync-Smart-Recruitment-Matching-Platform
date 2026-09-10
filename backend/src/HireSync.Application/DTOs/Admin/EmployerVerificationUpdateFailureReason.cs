namespace HireSync.Application.DTOs.Admin;

public enum EmployerVerificationUpdateFailureReason : byte
{
    NotFound = 1,
    InvalidTransition = 2,
    PersistenceFailed = 3
}
