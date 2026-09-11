using HireSync.Domain.Enums;

namespace HireSync.Domain.Rules;

public static class ContactRequestRules
{
    public static bool CanRespond(
        ContactRequestStatus currentStatus,
        ContactRequestStatus requestedStatus)
    {
        return currentStatus == ContactRequestStatus.Pending
            && requestedStatus is
                ContactRequestStatus.Accepted
                or ContactRequestStatus.Declined;
    }

    public static bool IsTerminal(ContactRequestStatus status)
    {
        return status is
            ContactRequestStatus.Accepted
            or ContactRequestStatus.Declined;
    }
}
