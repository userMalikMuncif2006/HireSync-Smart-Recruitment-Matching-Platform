using HireSync.Application.DTOs.Vacancy;

namespace HireSync.Application.Interfaces.Employer;

public interface IVacancyService
{
    Task<VacancyCreateResult> CreateOwnVacancyAsync(
        Guid employerUserId,
        CreateVacancyRequest request,
        CancellationToken cancellationToken = default);

    Task<VacancyStatusUpdateResult> UpdateOwnVacancyStatusAsync(
        Guid employerUserId,
        Guid vacancyId,
        UpdateVacancyStatusRequest request,
        CancellationToken cancellationToken = default);
}