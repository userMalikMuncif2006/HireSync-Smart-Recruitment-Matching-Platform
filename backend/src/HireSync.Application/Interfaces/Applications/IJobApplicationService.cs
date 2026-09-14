using HireSync.Application.DTOs.Applications;

namespace HireSync.Application.Interfaces.Applications;

public interface IJobApplicationService
{
    Task<ApplicationCreateResult> CreateAsync(
        Guid jobSeekerUserId,
        Guid vacancyId,
        CancellationToken cancellationToken = default);
}