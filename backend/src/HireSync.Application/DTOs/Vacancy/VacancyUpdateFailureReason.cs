namespace HireSync.Application.DTOs.Vacancy;

public enum VacancyUpdateFailureReason
{
    None = 0,
    InvalidInput = 1,
    NotFound = 2,
    Closed = 3,
    InvalidRequiredSkills = 4,
    ConcurrencyConflict = 5,
    PersistenceFailed = 6
}