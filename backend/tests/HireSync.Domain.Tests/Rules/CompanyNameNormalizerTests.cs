using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests.Rules;

public sealed class CompanyNameNormalizerTests
{
    [Theory]
    [InlineData("  Acme Holdings  ", "ACME HOLDINGS")]
    [InlineData("Acme     Holdings", "ACME HOLDINGS")]
    [InlineData("Acme\tHoldings", "ACME HOLDINGS")]
    [InlineData("AcMe Holdings", "ACME HOLDINGS")]
    [InlineData("A&B (Pvt) Ltd.", "A&B (PVT) LTD.")]
    [InlineData("ABC-Lanka", "ABC-LANKA")]
    public void Normalize_ReturnsCanonicalComparisonValue(
        string input,
        string expected)
    {
        var result = CompanyNameNormalizer.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_AppliesNfkcCompatibilityNormalization()
    {
        var result =
            CompanyNameNormalizer.Normalize("Ａｃｍｅ Holdings");

        Assert.Equal("ACME HOLDINGS", result);
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        var normalized =
            CompanyNameNormalizer.Normalize(
                "  Acme   Holdings (Pvt) Ltd. ");

        var secondPass =
            CompanyNameNormalizer.Normalize(normalized);

        Assert.Equal(normalized, secondPass);
    }

    [Fact]
    public void Normalize_AcceptsMinimumLength()
    {
        Assert.Equal(
            "AB",
            CompanyNameNormalizer.Normalize("ab"));
    }

    [Fact]
    public void Normalize_AcceptsMaximumLength()
    {
        var value = new string('A', 150);

        Assert.Equal(
            value,
            CompanyNameNormalizer.Normalize(value));
    }

    [Fact]
    public void Normalize_RejectsBelowMinimumLength()
    {
        Assert.Throws<ArgumentException>(
            () => CompanyNameNormalizer.Normalize("A"));
    }

    [Fact]
    public void Normalize_RejectsAboveMaximumLength()
    {
        var value = new string('A', 151);

        Assert.Throws<ArgumentException>(
            () => CompanyNameNormalizer.Normalize(value));
    }

    [Fact]
    public void CanonicalizeDisplayName_PreservesDisplayCasing()
    {
        var result =
            CompanyNameNormalizer.CanonicalizeDisplayName(
                "  Acme   Holdings (Pvt) Ltd.  ");

        Assert.Equal(
            "Acme Holdings (Pvt) Ltd.",
            result);
    }
}