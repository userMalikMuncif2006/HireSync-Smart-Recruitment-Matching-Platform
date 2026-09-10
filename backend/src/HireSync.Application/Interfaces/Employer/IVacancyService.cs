using HireSync.Application.DTOs.Vacancy;

namespace HireSync.Application.Interfaces.Employer;

public interface IVacancyService
{
    Task<VacancyStatusUpdateResult> UpdateOwnVacancyStatusAsync(
        Guid employerUserId,
        Guid vacancyId,
        UpdateVacancyStatusRequest request,
        CancellationToken cancellationToken = default);
}