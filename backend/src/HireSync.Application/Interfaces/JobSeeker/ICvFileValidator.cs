using HireSync.Application.DTOs;

namespace HireSync.Application.Interfaces.JobSeeker;

public interface ICvFileValidator
{
    Task<CvFileValidationResult> ValidateAsync(
        Stream content,
        string originalFileName,
        string declaredContentType,
        CancellationToken cancellationToken = default);
}
