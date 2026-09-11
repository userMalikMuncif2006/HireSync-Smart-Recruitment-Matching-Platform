using HireSync.Application.DTOs.Vacancy;

namespace HireSync.Application.Interfaces.Vacancy;

public interface IVacancySearchService
{
    Task<PublicVacancyPageDto> SearchOpenVacanciesAsync(
        SearchVacanciesRequest request,
        CancellationToken cancellationToken = default);
}