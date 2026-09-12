using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Entities;

public sealed class ContactRequest
{
    public Guid Id { get; private set; }

    public Guid JobApplicationId { get; private set; }

    public ContactRequestStatus Status { get; private set; }

    public DateTime RequestedAtUtc { get; private set; }

    public DateTime? RespondedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private ContactRequest()
    {
    }

    public ContactRequest(
        Guid id,
        Guid jobApplicationId,
        DateTime requestedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Contact request ID cannot be empty.",
                nameof(id));
        }

        if (jobApplicationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Job application ID cannot be empty.",
                nameof(jobApplicationId));
        }

        EnsureUtc(
            requestedAtUtc,
            nameof(requestedAtUtc));

        Id = id;
        JobApplicationId = jobApplicationId;
        Status = ContactRequestStatus.Pending;
        RequestedAtUtc = requestedAtUtc;
        RespondedAtUtc = null;
    }

    public void Respond(
        ContactRequestStatus requestedStatus,
        DateTime respondedAtUtc)
    {
        if (!Enum.IsDefined(
                typeof(ContactRequestStatus),
                requestedStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedStatus),
                "Contact request status is invalid.");
        }

        if (!ContactRequestRules.CanRespond(
                Status,
                requestedStatus))
        {
            throw new InvalidOperationException(
                $"Contact request cannot transition from {Status} to {requestedStatus}.");
        }

        EnsureUtc(
            respondedAtUtc,
            nameof(respondedAtUtc));

        Status = requestedStatus;
        RespondedAtUtc = respondedAtUtc;
    }

    private static void EnsureUtc(
        DateTime value,
        string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Timestamp must be UTC.",
                parameterName);
        }
    }
}
