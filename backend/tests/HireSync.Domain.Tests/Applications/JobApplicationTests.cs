using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests.Applications;

public sealed class JobApplicationTests
{
    [Fact]
    public void Constructor_CreatesAppliedApplication()
    {
        var now = Utc(1);

        var application = Create(now);

        Assert.Equal(ApplicationStatus.Applied, application.Status);
        Assert.Equal(now, application.AppliedAtUtc);
        Assert.Equal(now, application.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Selected)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Selected)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Selected)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Rejected)]
    public void CanTransition_ApprovedEdges_ReturnTrue(
        ApplicationStatus current,
        ApplicationStatus requested)
    {
        Assert.True(
            ApplicationStatusRules.CanTransition(
                current,
                requested));
    }

    [Theory]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Selected, ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Selected, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Selected)]
    public void CanTransition_DisallowedEdges_ReturnFalse(
        ApplicationStatus current,
        ApplicationStatus requested)
    {
        Assert.False(
            ApplicationStatusRules.CanTransition(
                current,
                requested));
    }

    [Theory]
    [InlineData(ApplicationStatus.Selected)]
    [InlineData(ApplicationStatus.Rejected)]
    public void IsTerminal_ReturnsTrueForTerminalStates(
        ApplicationStatus status)
    {
        Assert.True(
            ApplicationStatusRules.IsTerminal(status));
    }

    [Fact]
    public void ChangeStatus_ValidTransition_UpdatesStatusAndTimestamp()
    {
        var application = Create(Utc(1));
        var changedAt = Utc(2);

        var changed = application.ChangeStatus(
            ApplicationStatus.UnderReview,
            changedAt);

        Assert.True(changed);
        Assert.Equal(
            ApplicationStatus.UnderReview,
            application.Status);
        Assert.Equal(changedAt, application.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeStatus_SameState_IsNoOp()
    {
        var originalTime = Utc(1);
        var application = Create(originalTime);

        var changed = application.ChangeStatus(
            ApplicationStatus.Applied,
            Utc(2));

        Assert.False(changed);
        Assert.Equal(
            ApplicationStatus.Applied,
            application.Status);
        Assert.Equal(
            originalTime,
            application.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeStatus_InvalidTransition_ThrowsWithoutMutation()
    {
        var application = Create(Utc(1));

        application.ChangeStatus(
            ApplicationStatus.Shortlisted,
            Utc(2));

        Assert.Throws<InvalidOperationException>(
            () => application.ChangeStatus(
                ApplicationStatus.UnderReview,
                Utc(3)));

        Assert.Equal(
            ApplicationStatus.Shortlisted,
            application.Status);
        Assert.Equal(Utc(2), application.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeStatus_TerminalState_CannotTransitionAgain()
    {
        var application = Create(Utc(1));

        application.ChangeStatus(
            ApplicationStatus.Selected,
            Utc(2));

        Assert.Throws<InvalidOperationException>(
            () => application.ChangeStatus(
                ApplicationStatus.Rejected,
                Utc(3)));
    }

    [Fact]
    public void Constructor_NonUtcTimestamp_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => Create(
                DateTime.SpecifyKind(
                    DateTime.UtcNow,
                    DateTimeKind.Local)));
    }

    [Fact]
    public void EnumValues_AreCanonical()
    {
        Assert.Equal(1, (byte)ApplicationStatus.Applied);
        Assert.Equal(2, (byte)ApplicationStatus.UnderReview);
        Assert.Equal(3, (byte)ApplicationStatus.Shortlisted);
        Assert.Equal(4, (byte)ApplicationStatus.Selected);
        Assert.Equal(5, (byte)ApplicationStatus.Rejected);
    }

    private static JobApplication Create(
        DateTime appliedAtUtc)
    {
        return new JobApplication(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Guid.Parse("30000000-0000-0000-0000-000000000001"),
            appliedAtUtc);
    }

    private static DateTime Utc(int day)
    {
        return new DateTime(
            2026,
            9,
            day,
            12,
            0,
            0,
            DateTimeKind.Utc);
    }
}
