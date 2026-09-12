using HireSync.Application.DTOs;

namespace HireSync.Application.Interfaces.JobSeeker;

public interface IJobSeekerCvService
{
    Task<JobSeekerCvDto?> GetOwnCvAsync(
        CancellationToken cancellationToken = default);

    Task<CvUploadResult> UploadOrReplaceOwnCvAsync(
        CvUploadRequest request,
        CancellationToken cancellationToken = default);
}
