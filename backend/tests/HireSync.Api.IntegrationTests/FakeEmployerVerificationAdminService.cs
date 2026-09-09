using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;

namespace HireSync.Api.IntegrationTests;

internal sealed class FakeEmployerVerificationAdminService
    : IEmployerVerificationAdminService
{
    public IReadOnlyList<EmployerVerificationSummaryDto> PendingEmployers
        { get; set; } = Array.Empty<EmployerVerificationSummaryDto>();

    public EmployerVerificationUpdateResult ApproveResult { get; set; } =
        EmployerVerificationUpdateResult.Failure(
            EmployerVerificationUpdateFailureReason.NotFound);

    public EmployerVerificationUpdateResult RejectResult { get; set; } =
        EmployerVerificationUpdateResult.Failure(
            EmployerVerificationUpdateFailureReason.NotFound);

    public Task<IReadOnlyList<EmployerVerificationSummaryDto>>
        GetPendingEmployersAsync(
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PendingEmployers);
    }

    public Task<EmployerVerificationUpdateResult> ApproveAsync(
        Guid employerUserId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApproveResult);
    }

    public Task<EmployerVerificationUpdateResult> RejectAsync(
        Guid employerUserId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RejectResult);
    }
}
