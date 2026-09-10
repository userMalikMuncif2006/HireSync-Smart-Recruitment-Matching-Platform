using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests.Rules;

public class EmployerVacancyReadinessRulesTests
{
    [Fact]
    public void IsVacancyReady_ReturnsTrue_WhenAllRequirementsAreMet()
    {
        var result =
            EmployerVacancyReadinessRules.IsVacancyReady(
                AccountStatus.Active,
                EmployerVerificationStatus.Approved,
                isProfileComplete: true);

        Assert.True(result);
    }

    [Fact]
    public void IsVacancyReady_ReturnsFalse_WhenAccountIsSuspended()
    {
        var result =
            EmployerVacancyReadinessRules.IsVacancyReady(
                AccountStatus.Suspended,
                EmployerVerificationStatus.Approved,
                isProfileComplete: true);

        Assert.False(result);
    }

    [Fact]
    public void IsVacancyReady_ReturnsFalse_WhenVerificationIsPending()
    {
        var result =
            EmployerVacancyReadinessRules.IsVacancyReady(
                AccountStatus.Active,
                EmployerVerificationStatus.Pending,
                isProfileComplete: true);

        Assert.False(result);
    }

    [Fact]
    public void IsVacancyReady_ReturnsFalse_WhenVerificationIsRejected()
    {
        var result =
            EmployerVacancyReadinessRules.IsVacancyReady(
                AccountStatus.Active,
                EmployerVerificationStatus.Rejected,
                isProfileComplete: true);

        Assert.False(result);
    }

    [Fact]
    public void IsVacancyReady_ReturnsFalse_WhenVerificationIsNull()
    {
        var result =
            EmployerVacancyReadinessRules.IsVacancyReady(
                AccountStatus.Active,
                verificationStatus: null,
                isProfileComplete: true);

        Assert.False(result);
    }

    [Fact]
    public void IsVacancyReady_ReturnsFalse_WhenProfileIsIncomplete()
    {
        var result =
            EmployerVacancyReadinessRules.IsVacancyReady(
                AccountStatus.Active,
                EmployerVerificationStatus.Approved,
                isProfileComplete: false);

        Assert.False(result);
    }
}