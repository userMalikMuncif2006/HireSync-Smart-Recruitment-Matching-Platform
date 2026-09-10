using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests.Rules;

public sealed class EmployerProfileRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void HasValidRequiredText_ReturnsFalse_ForMissingValue(
        string? value)
    {
        var result = EmployerProfileRules.HasValidRequiredText(
            value,
            2,
            100);

        Assert.False(result);
    }

    [Fact]
    public void HasValidRequiredText_ReturnsTrue_AtMinimumLength()
    {
        var result = EmployerProfileRules.HasValidRequiredText(
            "AB",
            2,
            100);

        Assert.True(result);
    }

    [Fact]
    public void HasValidRequiredText_ReturnsFalse_WhenTooLong()
    {
        var result = EmployerProfileRules.HasValidRequiredText(
            new string('A', 101),
            2,
            100);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://hiresync.example")]
    [InlineData("http://hiresync.example")]
    public void IsValidCompanyWebsite_ReturnsTrue_ForAllowedValues(
        string? website)
    {
        Assert.True(
            EmployerProfileRules.IsValidCompanyWebsite(website));
    }

    [Theory]
    [InlineData("ftp://hiresync.example")]
    [InlineData("not-a-url")]
    public void IsValidCompanyWebsite_ReturnsFalse_ForInvalidValues(
        string website)
    {
        Assert.False(
            EmployerProfileRules.IsValidCompanyWebsite(website));
    }
}