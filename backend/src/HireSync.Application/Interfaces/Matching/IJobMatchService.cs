using HireSync.Application.DTOs.Matching;

namespace HireSync.Application.Interfaces.Matching;

public interface IJobMatchService
{
    Task<JobMatchQueryResult> GetMatchAsync(
        Guid jobSeekerUserId,
        Guid vacancyId,
        CancellationToken cancellationToken = default);
}
