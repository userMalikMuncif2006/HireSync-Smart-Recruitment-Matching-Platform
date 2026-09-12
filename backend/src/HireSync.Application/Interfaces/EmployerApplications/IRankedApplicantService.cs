using HireSync.Application.DTOs.EmployerApplications;

namespace HireSync.Application.Interfaces.EmployerApplications;

public interface IRankedApplicantService
{
    Task<RankedApplicantQueryResult>
        GetOwnVacancyApplicantsAsync(
            Guid employerUserId,
            Guid vacancyId,
            RankedApplicantListRequest request,
            CancellationToken cancellationToken = default);
}