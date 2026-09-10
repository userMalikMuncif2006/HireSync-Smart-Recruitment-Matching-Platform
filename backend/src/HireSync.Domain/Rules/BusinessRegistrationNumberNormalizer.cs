using System.Text;

namespace HireSync.Domain.Rules;

public static class BusinessRegistrationNumberNormalizer
{
    public static string CanonicalizeDisplayValue(
        string businessRegistrationNumber)
    {
        ArgumentNullException.ThrowIfNull(businessRegistrationNumber);

        var unicodeNormalized =
            businessRegistrationNumber.Normalize(
                NormalizationForm.FormKC);

        return CollapseWhitespace(unicodeNormalized);
    }

    public static string Normalize(
        string businessRegistrationNumber)
    {
        var normalized =
            CanonicalizeDisplayValue(businessRegistrationNumber)
                .ToUpperInvariant();

        if (normalized.Length <
                EmployerProfileRules
                    .BusinessRegistrationNumberMinLength ||
            normalized.Length >
                EmployerProfileRules
                    .BusinessRegistrationNumberMaxLength)
        {
            throw new ArgumentException(
                $"Normalized business registration number must be " +
                $"between " +
                $"{EmployerProfileRules.BusinessRegistrationNumberMinLength} " +
                $"and " +
                $"{EmployerProfileRules.BusinessRegistrationNumberMaxLength} " +
                $"characters.",
                nameof(businessRegistrationNumber));
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