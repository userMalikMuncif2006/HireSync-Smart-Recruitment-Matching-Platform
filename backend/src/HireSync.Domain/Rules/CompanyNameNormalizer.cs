using System.Text;

namespace HireSync.Domain.Rules;

public static class CompanyNameNormalizer
{
    public static string CanonicalizeDisplayName(string companyName)
    {
        ArgumentNullException.ThrowIfNull(companyName);

        var unicodeNormalized =
            companyName.Normalize(NormalizationForm.FormKC);

        return CollapseWhitespace(unicodeNormalized);
    }

    public static string Normalize(string companyName)
    {
        var normalized =
            CanonicalizeDisplayName(companyName)
                .ToUpperInvariant();

        if (normalized.Length <
                EmployerProfileRules.CompanyNameMinLength ||
            normalized.Length >
                EmployerProfileRules.CompanyNameMaxLength)
        {
            throw new ArgumentException(
                $"Normalized company name must be between " +
                $"{EmployerProfileRules.CompanyNameMinLength} and " +
                $"{EmployerProfileRules.CompanyNameMaxLength} characters.",
                nameof(companyName));
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