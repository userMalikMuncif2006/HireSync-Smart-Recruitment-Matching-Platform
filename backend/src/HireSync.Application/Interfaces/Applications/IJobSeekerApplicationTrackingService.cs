using HireSync.Application.DTOs.Applications;

namespace HireSync.Application.Interfaces.Applications;

public interface IJobSeekerApplicationTrackingService
{
    Task<JobSeekerApplicationTrackingResult>
        GetOwnApplicationsAsync(
            Guid jobSeekerUserId,
            JobSeekerApplicationListRequest request,
            CancellationToken cancellationToken = default);
}
