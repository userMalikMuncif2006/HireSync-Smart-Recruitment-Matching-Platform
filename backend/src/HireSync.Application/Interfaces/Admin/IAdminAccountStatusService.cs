using HireSync.Application.DTOs.Admin;

namespace HireSync.Application.Interfaces.Admin;

public interface IAdminAccountStatusService
{
    Task<AdminAccountStatusUpdateResult> UpdateStatusAsync(
        Guid administratorUserId,
        Guid targetUserId,
        UpdateAdminAccountStatusRequest request,
        CancellationToken cancellationToken = default);
}