using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests;

public class EmployerVerificationPolicyTests
{
    [Fact]
    public void Pending_can_be_approved()
    {
        Assert.True(
            EmployerVerificationPolicy.CanTransition(
                EmployerVerificationStatus.Pending,
                EmployerVerificationStatus.Approved));
    }

    [Fact]
    public void Pending_can_be_rejected()
    {
        Assert.True(
            EmployerVerificationPolicy.CanTransition(
                EmployerVerificationStatus.Pending,
                EmployerVerificationStatus.Rejected));
    }

    [Theory]
    [InlineData(EmployerVerificationStatus.Approved, EmployerVerificationStatus.Rejected)]
    [InlineData(EmployerVerificationStatus.Rejected, EmployerVerificationStatus.Approved)]
    [InlineData(EmployerVerificationStatus.Approved, EmployerVerificationStatus.Approved)]
    [InlineData(EmployerVerificationStatus.Rejected, EmployerVerificationStatus.Rejected)]
    [InlineData(EmployerVerificationStatus.Pending, EmployerVerificationStatus.Pending)]
    public void Other_transitions_are_not_allowed(
        EmployerVerificationStatus current,
        EmployerVerificationStatus target)
    {
        Assert.False(
            EmployerVerificationPolicy.CanTransition(current, target));
    }
}
