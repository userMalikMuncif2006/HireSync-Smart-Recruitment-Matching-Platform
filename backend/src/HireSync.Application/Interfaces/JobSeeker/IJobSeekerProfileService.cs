using HireSync.Application.DTOs;

namespace HireSync.Application.Interfaces.JobSeeker;

public interface IJobSeekerProfileService
{
    Task<JobSeekerProfileDto?> GetOwnProfileAsync(
        CancellationToken cancellationToken = default);

    Task<JobSeekerProfileUpdateResult> UpdateOwnProfileAsync(
        UpdateJobSeekerProfileRequest request,
        CancellationToken cancellationToken = default);
}