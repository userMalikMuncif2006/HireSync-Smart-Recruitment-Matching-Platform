using HireSync.Application.DTOs.Matching;

namespace HireSync.Application.Interfaces.Matching;

public interface IVacancyDetailService
{
    Task<VacancyDetailQueryResult> GetDetailAsync(
        Guid jobSeekerUserId,
        Guid vacancyId,
        CancellationToken cancellationToken = default);
}