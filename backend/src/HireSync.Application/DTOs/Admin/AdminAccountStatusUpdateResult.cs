namespace HireSync.Application.DTOs.Admin;

public enum AdminAccountStatusUpdateFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    NotFound = 2,
    ProtectedTarget = 3,
    ConcurrencyConflict = 4,
    PersistenceFailed = 5
}

public sealed record AdminAccountStatusUpdateResult(
    bool Succeeded,
    AdminUserListItemDto? User,
    AdminAccountStatusUpdateFailureReason FailureReason)
{
    public static AdminAccountStatusUpdateResult Success(
        AdminUserListItemDto user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new AdminAccountStatusUpdateResult(
            true,
            user,
            AdminAccountStatusUpdateFailureReason.None);
    }

    public static AdminAccountStatusUpdateResult Failure(
        AdminAccountStatusUpdateFailureReason reason)
    {
        if (reason ==
            AdminAccountStatusUpdateFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new AdminAccountStatusUpdateResult(
            false,
            null,
            reason);
    }
}