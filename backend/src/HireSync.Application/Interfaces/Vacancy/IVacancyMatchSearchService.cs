using HireSync.Application.DTOs.Vacancy;

namespace HireSync.Application.Interfaces.Vacancy;

public interface IVacancyMatchSearchService
{
    Task<VacancySearchQueryResult>
        SearchOpenVacanciesByMatchAsync(
            Guid jobSeekerUserId,
            SearchVacanciesRequest request,
            CancellationToken cancellationToken = default);
}