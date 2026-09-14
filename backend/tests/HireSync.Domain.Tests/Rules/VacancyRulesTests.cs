using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests.Rules;

public class VacancyRulesTests
{
    [Theory]
    [InlineData("Developer")]
    [InlineData("Software Engineer")]
    public void IsValidTitle_ReturnsTrue_ForValidValues(string title)
    {
        Assert.True(VacancyRules.IsValidTitle(title));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("AB")]
    public void IsValidTitle_ReturnsFalse_ForInvalidValues(string? title)
    {
        Assert.False(VacancyRules.IsValidTitle(title));
    }

    [Fact]
    public void IsValidTitle_ReturnsFalse_WhenLongerThanMaximum()
    {
        var title = new string('A', VacancyRules.TitleMaxLength + 1);

        Assert.False(VacancyRules.IsValidTitle(title));
    }

    [Fact]
    public void IsValidDescription_ReturnsTrue_AtMinimumLength()
    {
        var description =
            new string('A', VacancyRules.DescriptionMinLength);

        Assert.True(
            VacancyRules.IsValidDescription(description));
    }

    [Fact]
    public void IsValidDescription_ReturnsFalse_WhenTooShort()
    {
        var description =
            new string(
                'A',
                VacancyRules.DescriptionMinLength - 1);

        Assert.False(
            VacancyRules.IsValidDescription(description));
    }

    [Theory]
    [InlineData("Colombo")]
    [InlineData("Kandy")]
    public void IsValidLocation_ReturnsTrue_ForValidValues(
        string location)
    {
        Assert.True(VacancyRules.IsValidLocation(location));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void IsValidLocation_ReturnsFalse_ForInvalidValues(
        string? location)
    {
        Assert.False(VacancyRules.IsValidLocation(location));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(720)]
    public void IsValidMinimumExperienceMonths_ReturnsTrue_InRange(
        int months)
    {
        Assert.True(
            VacancyRules.IsValidMinimumExperienceMonths(months));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(721)]
    public void IsValidMinimumExperienceMonths_ReturnsFalse_OutOfRange(
        int months)
    {
        Assert.False(
            VacancyRules.IsValidMinimumExperienceMonths(months));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(50)]
    public void IsValidRequiredSkillCount_ReturnsTrue_InRange(
        int count)
    {
        Assert.True(
            VacancyRules.IsValidRequiredSkillCount(count));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    [InlineData(-1)]
    public void IsValidRequiredSkillCount_ReturnsFalse_OutOfRange(
        int count)
    {
        Assert.False(
            VacancyRules.IsValidRequiredSkillCount(count));
    }

    [Fact]
    public void OpenVacancy_CanBeUpdated()
    {
        Assert.True(
            VacancyRules.CanUpdate(VacancyStatus.Open));
    }

    [Fact]
    public void ClosedVacancy_CannotBeUpdated()
    {
        Assert.False(
            VacancyRules.CanUpdate(VacancyStatus.Closed));
    }

    [Fact]
    public void OpenVacancy_CanTransitionToClosed()
    {
        Assert.True(
            VacancyRules.CanTransition(
                VacancyStatus.Open,
                VacancyStatus.Closed));
    }

    [Fact]
    public void ClosedVacancy_CannotTransitionToOpen()
    {
        Assert.False(
            VacancyRules.CanTransition(
                VacancyStatus.Closed,
                VacancyStatus.Open));
    }

    [Fact]
    public void RepeatingClosedStatus_IsNotAValidTransition()
    {
        Assert.False(
            VacancyRules.CanTransition(
                VacancyStatus.Closed,
                VacancyStatus.Closed));
    }

    [Fact]
    public void ClosedVacancy_IsTerminal()
    {
        Assert.True(
            VacancyRules.IsTerminal(VacancyStatus.Closed));
    }

    [Fact]
    public void OpenVacancy_IsNotTerminal()
    {
        Assert.False(
            VacancyRules.IsTerminal(VacancyStatus.Open));
    }
}