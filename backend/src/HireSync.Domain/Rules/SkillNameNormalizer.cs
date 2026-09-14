using System.Text;

namespace HireSync.Domain.Rules;

public static class SkillNameNormalizer
{
    public const int MaxLength = 50;

    public static string CanonicalizeDisplayName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var unicodeNormalized = name.Normalize(NormalizationForm.FormKC);
        var collapsed = CollapseWhitespace(unicodeNormalized);

        if (collapsed.Length is < 1 or > MaxLength)
        {
            throw new ArgumentException(
                $"Skill name must be between 1 and {MaxLength} characters after normalization.",
                nameof(name));
        }

        return collapsed;
    }

    public static string Normalize(string name)
    {
        var normalized = CanonicalizeDisplayName(name).ToUpperInvariant();

        if (normalized.Length is < 1 or > MaxLength)
        {
            throw new ArgumentException(
                $"Normalized skill name must be between 1 and {MaxLength} characters.",
                nameof(name));
        }

        return normalized;
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}