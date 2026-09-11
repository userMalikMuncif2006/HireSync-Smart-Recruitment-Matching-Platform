using HireSync.Domain.Enums;

namespace HireSync.Domain.Rules;

public static class EmployerVacancyReadinessRules
{
    public static bool IsVacancyReady(
        AccountStatus accountStatus,
        EmployerVerificationStatus? verificationStatus,
        bool isProfileComplete)
    {
        return accountStatus == AccountStatus.Active &&
               verificationStatus == EmployerVerificationStatus.Approved &&
               isProfileComplete;
    }
}