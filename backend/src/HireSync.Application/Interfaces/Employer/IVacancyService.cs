using HireSync.Application.DTOs.Vacancy;

namespace HireSync.Application.Interfaces.Employer;

public interface IVacancyService
{
    Task<EmployerVacancyPageDto?> GetOwnVacanciesAsync(
        Guid employerUserId,
        EmployerVacancyListRequest request,
        CancellationToken cancellationToken = default);

    Task<VacancyDto?> GetOwnVacancyAsync(
        Guid employerUserId,
        Guid vacancyId,
        CancellationToken cancellationToken = default);

    Task<VacancyCreateResult> CreateOwnVacancyAsync(
        Guid employerUserId,
        CreateVacancyRequest request,
        CancellationToken cancellationToken = default);

    Task<VacancyUpdateResult> UpdateOwnVacancyAsync(
        Guid employerUserId,
        Guid vacancyId,
        UpdateVacancyRequest request,
        CancellationToken cancellationToken = default);

    Task<VacancyStatusUpdateResult> UpdateOwnVacancyStatusAsync(
        Guid employerUserId,
        Guid vacancyId,
        UpdateVacancyStatusRequest request,
        CancellationToken cancellationToken = default);
}