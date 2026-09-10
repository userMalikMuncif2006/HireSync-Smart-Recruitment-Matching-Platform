namespace HireSync.Application.DTOs.Vacancy;

public enum VacancyStatusUpdateFailureReason
{
    None = 0,
    NotFound = 1,
    InvalidInput = 2,
    AlreadyClosed = 3,
    ConcurrencyConflict = 4,
    PersistenceFailed = 5
}