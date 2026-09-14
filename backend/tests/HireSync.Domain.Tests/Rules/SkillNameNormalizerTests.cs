using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests.Rules;

public sealed class SkillNameNormalizerTests
{
    [Fact]
    public void Normalize_TrimsCollapsesWhitespaceAndUppercases()
    {
        var result = SkillNameNormalizer.Normalize("  c#   developer  ");

        Assert.Equal("C# DEVELOPER", result);
    }

    [Fact]
    public void Normalize_PreservesMeaningfulPunctuation()
    {
        var dotNet = SkillNameNormalizer.Normalize("  .NET  ");
        var dotNetWord = SkillNameNormalizer.Normalize("DOTNET");

        Assert.Equal(".NET", dotNet);
        Assert.Equal("DOTNET", dotNetWord);
        Assert.NotEqual(dotNet, dotNetWord);
    }

    [Fact]
    public void Normalize_AppliesUnicodeNfkcNormalization()
    {
        var result = SkillNameNormalizer.Normalize("  Ｃ＃  ");

        Assert.Equal("C#", result);
    }

    [Fact]
    public void CanonicalizeDisplayName_PreservesDisplayCaseAndCollapsesWhitespace()
    {
        var result = SkillNameNormalizer.CanonicalizeDisplayName("  C#   Developer  ");

        Assert.Equal("C# Developer", result);
    }

    [Fact]
    public void Normalize_WhitespaceOnly_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => SkillNameNormalizer.Normalize("   "));
    }

    [Fact]
    public void Normalize_MoreThanFiftyCharacters_ThrowsArgumentException()
    {
        var value = new string('a', 51);

        Assert.Throws<ArgumentException>(
            () => SkillNameNormalizer.Normalize(value));
    }
}