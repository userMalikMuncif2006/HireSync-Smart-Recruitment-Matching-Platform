using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests.Rules;

public sealed class LocationNormalizerTests
{
    [Fact]
    public void Normalize_TrimsCollapsesWhitespaceAndUppercases()
    {
        var result = LocationNormalizer.Normalize("  Colombo   03  ");

        Assert.Equal("COLOMBO 03", result);
    }

    [Fact]
    public void CanonicalizeDisplayLocation_PreservesDisplayCase()
    {
        var result = LocationNormalizer.CanonicalizeDisplayLocation(
            "  Colombo   Central  ");

        Assert.Equal("Colombo Central", result);
    }

    [Fact]
    public void Normalize_AppliesUnicodeNfkcNormalization()
    {
        var result = LocationNormalizer.Normalize("  ＣＯＬＯＭＢＯ  ");

        Assert.Equal("COLOMBO", result);
    }

    [Fact]
    public void Normalize_PreservesPunctuation()
    {
        var result = LocationNormalizer.Normalize(
            "  Colombo-03, Western  ");

        Assert.Equal("COLOMBO-03, WESTERN", result);
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        var once = LocationNormalizer.Normalize(
            "  Colombo   Central  ");

        var twice = LocationNormalizer.Normalize(once);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Normalize_ExactlyOneHundredCharacters_IsAccepted()
    {
        var value = new string('a', 100);

        var result = LocationNormalizer.Normalize(value);

        Assert.Equal(100, result.Length);
    }

    [Fact]
    public void Normalize_MoreThanOneHundredCharacters_ThrowsArgumentException()
    {
        var value = new string('a', 101);

        Assert.Throws<ArgumentException>(
            () => LocationNormalizer.Normalize(value));
    }

    [Fact]
    public void Normalize_WhitespaceOnly_ReturnsEmptyString()
    {
        var result = LocationNormalizer.Normalize("   ");

        Assert.Equal(string.Empty, result);
    }
}