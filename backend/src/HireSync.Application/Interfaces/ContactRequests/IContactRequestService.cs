using HireSync.Application.DTOs.ContactRequests;

namespace HireSync.Application.Interfaces.ContactRequests;

public interface IContactRequestService
{
    Task<IReadOnlyList<JobSeekerContactRequestDto>>
        GetForOwnJobSeekerAsync(
            Guid jobSeekerUserId,
            CancellationToken cancellationToken = default);

    Task<ContactRequestWriteResult>
        CreateForOwnApplicationAsync(
            Guid employerUserId,
            Guid jobApplicationId,
            CancellationToken cancellationToken = default);

    Task<ContactRequestWriteResult>
        RespondToOwnContactRequestAsync(
            Guid jobSeekerUserId,
            Guid contactRequestId,
            RespondContactRequestRequest request,
            CancellationToken cancellationToken = default);
}
