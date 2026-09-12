namespace HireSync.Application.DTOs.ContactRequests;

public enum ContactRequestWriteFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    NotFound = 2,
    ApplicationRejected = 3,
    ParticipantInactive = 4,
    AlreadyExists = 5,
    InvalidTransition = 6,
    ConcurrencyConflict = 7,
    PersistenceFailed = 8
}

public sealed record ContactRequestWriteResult(
    bool Succeeded,
    ContactRequestDto? ContactRequest,
    ContactRequestWriteFailureReason FailureReason)
{
    public static ContactRequestWriteResult Success(
        ContactRequestDto contactRequest)
    {
        ArgumentNullException.ThrowIfNull(contactRequest);

        return new ContactRequestWriteResult(
            true,
            contactRequest,
            ContactRequestWriteFailureReason.None);
    }

    public static ContactRequestWriteResult Failure(
        ContactRequestWriteFailureReason reason)
    {
        if (reason == ContactRequestWriteFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new ContactRequestWriteResult(
            false,
            null,
            reason);
    }
}
