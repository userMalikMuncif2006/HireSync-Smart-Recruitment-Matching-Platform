using HireSync.Application.DTOs.Applications;

namespace HireSync.Application.Interfaces.Applications;

public interface IApplicationStatusService
{
    Task<ApplicationStatusUpdateResult>
        UpdateOwnApplicationStatusAsync(
            Guid employerUserId,
            Guid applicationId,
            UpdateApplicationStatusRequest request,
            CancellationToken cancellationToken = default);
}
