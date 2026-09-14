namespace HireSync.Application.DTOs.Vacancy;

public enum VacancyCreateFailureReason
{
    None = 0,
    InvalidInput = 1,
    EmployerNotReady = 2,
    InvalidRequiredSkills = 3,
    PersistenceFailed = 4
}