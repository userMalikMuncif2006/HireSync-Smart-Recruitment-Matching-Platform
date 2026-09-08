using System.Text;

namespace HireSync.Domain.Rules;

public static class LocationNormalizer
{
    public const int MaxLength = 100;

    public static string CanonicalizeDisplayLocation(string location)
    {
        ArgumentNullException.ThrowIfNull(location);

        var unicodeNormalized = location.Normalize(NormalizationForm.FormKC);

        return CollapseWhitespace(unicodeNormalized);
    }

    public static string Normalize(string location)
    {
        var normalized = CanonicalizeDisplayLocation(location)
            .ToUpperInvariant();

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Normalized location must not exceed {MaxLength} characters.",
                nameof(location));
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