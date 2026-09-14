using HireSync.Application.DTOs.Admin;

namespace HireSync.Application.Interfaces.Admin;

public interface IEmployerVerificationAdminService
{
    Task<IReadOnlyList<EmployerVerificationSummaryDto>>
        GetPendingEmployersAsync(
            CancellationToken cancellationToken = default);

    Task<EmployerVerificationUpdateResult> ApproveAsync(
        Guid employerUserId,
        CancellationToken cancellationToken = default);

    Task<EmployerVerificationUpdateResult> RejectAsync(
        Guid employerUserId,
        CancellationToken cancellationToken = default);
}
