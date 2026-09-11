using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests.Rules;

public sealed class BusinessRegistrationNumberNormalizerTests
{
    [Theory]
    [InlineData("  PV 12345  ", "PV 12345")]
    [InlineData("PV    12345", "PV 12345")]
    [InlineData("pv 12345/a", "PV 12345/A")]
    [InlineData("PV-12345", "PV-12345")]
    [InlineData("PV/12345", "PV/12345")]
    public void Normalize_ReturnsCanonicalComparisonValue(
        string input,
        string expected)
    {
        var result =
            BusinessRegistrationNumberNormalizer.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_AppliesNfkcCompatibilityNormalization()
    {
        var result =
            BusinessRegistrationNumberNormalizer.Normalize(
                "ＰＶ １２３４５");

        Assert.Equal("PV 12345", result);
    }

    [Fact]
    public void Normalize_PreservesSeparatorDifferences()
    {
        var withSpace =
            BusinessRegistrationNumberNormalizer.Normalize(
                "PV 12345");

        var withoutSpace =
            BusinessRegistrationNumberNormalizer.Normalize(
                "PV12345");

        var withHyphen =
            BusinessRegistrationNumberNormalizer.Normalize(
                "PV-12345");

        var withSlash =
            BusinessRegistrationNumberNormalizer.Normalize(
                "PV/12345");

        Assert.NotEqual(withSpace, withoutSpace);
        Assert.NotEqual(withHyphen, withSlash);
        Assert.NotEqual(withSpace, withHyphen);
    }

    [Fact]
    public void Normalize_EquivalentFormattingProducesSameKey()
    {
        var first =
            BusinessRegistrationNumberNormalizer.Normalize(
                "pv   12345");

        var second =
            BusinessRegistrationNumberNormalizer.Normalize(
                " PV 12345 ");

        Assert.Equal("PV 12345", first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        var normalized =
            BusinessRegistrationNumberNormalizer.Normalize(
                " pv   12345/a ");

        var secondPass =
            BusinessRegistrationNumberNormalizer.Normalize(
                normalized);

        Assert.Equal(normalized, secondPass);
    }

    [Fact]
    public void Normalize_AcceptsMinimumLength()
    {
        Assert.Equal(
            "AB",
            BusinessRegistrationNumberNormalizer.Normalize("ab"));
    }

    [Fact]
    public void Normalize_AcceptsMaximumLength()
    {
        var value = new string('A', 100);

        Assert.Equal(
            value,
            BusinessRegistrationNumberNormalizer.Normalize(value));
    }

    [Fact]
    public void Normalize_RejectsBelowMinimumLength()
    {
        Assert.Throws<ArgumentException>(
            () =>
                BusinessRegistrationNumberNormalizer.Normalize("A"));
    }

    [Fact]
    public void Normalize_RejectsAboveMaximumLength()
    {
        var value = new string('A', 101);

        Assert.Throws<ArgumentException>(
            () =>
                BusinessRegistrationNumberNormalizer.Normalize(value));
    }

    [Fact]
    public void CanonicalizeDisplayValue_PreservesDisplayCasing()
    {
        var result =
            BusinessRegistrationNumberNormalizer
                .CanonicalizeDisplayValue(
                    "  pv   12345/a ");

        Assert.Equal("pv 12345/a", result);
    }
}