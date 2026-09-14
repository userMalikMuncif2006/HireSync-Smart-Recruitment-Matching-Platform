using HireSync.Domain.Enums;

namespace HireSync.Domain.Rules;

public static class ApplicationStatusRules
{
    public static bool IsTerminal(ApplicationStatus status)
    {
        return status is
            ApplicationStatus.Selected
            or ApplicationStatus.Rejected;
    }

    public static bool IsSameState(
        ApplicationStatus currentStatus,
        ApplicationStatus requestedStatus)
    {
        return currentStatus == requestedStatus;
    }

    public static bool CanTransition(
        ApplicationStatus currentStatus,
        ApplicationStatus requestedStatus)
    {
        return currentStatus switch
        {
            ApplicationStatus.Applied =>
                requestedStatus is
                    ApplicationStatus.UnderReview
                    or ApplicationStatus.Shortlisted
                    or ApplicationStatus.Selected
                    or ApplicationStatus.Rejected,

            ApplicationStatus.UnderReview =>
                requestedStatus is
                    ApplicationStatus.Shortlisted
                    or ApplicationStatus.Selected
                    or ApplicationStatus.Rejected,

            ApplicationStatus.Shortlisted =>
                requestedStatus is
                    ApplicationStatus.Selected
                    or ApplicationStatus.Rejected,

            _ => false
        };
    }
}
