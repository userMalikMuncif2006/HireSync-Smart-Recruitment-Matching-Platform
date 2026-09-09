using HireSync.Domain.Enums;

namespace HireSync.Domain.Rules;

public static class EmployerVerificationPolicy
{
    public static bool CanTransition(
        EmployerVerificationStatus current,
        EmployerVerificationStatus target)
    {
        return current == EmployerVerificationStatus.Pending &&
               (target == EmployerVerificationStatus.Approved ||
                target == EmployerVerificationStatus.Rejected);
    }
}
