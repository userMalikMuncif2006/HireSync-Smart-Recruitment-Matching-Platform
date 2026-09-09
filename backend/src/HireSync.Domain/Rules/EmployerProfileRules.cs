namespace HireSync.Domain.Rules;

public static class EmployerProfileRules
{
    public const int CompanyNameMinLength = 2;
    public const int CompanyNameMaxLength = 150;

    public const int DescriptionMinLength = 20;
    public const int DescriptionMaxLength = 2000;

    public const int LocationMinLength = 2;
    public const int LocationMaxLength = 100;

    public const int BusinessRegistrationNumberMinLength = 2;
    public const int BusinessRegistrationNumberMaxLength = 100;

    public const int ContactPersonNameMinLength = 2;
    public const int ContactPersonNameMaxLength = 100;

    public const int ContactPersonDesignationMinLength = 2;
    public const int ContactPersonDesignationMaxLength = 100;

    public const int MobileNumberMinLength = 5;
    public const int MobileNumberMaxLength = 30;

    public const int CompanyWebsiteMaxLength = 300;

    public static bool HasValidRequiredText(
        string? value,
        int minLength,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        return trimmed.Length >= minLength &&
               trimmed.Length <= maxLength;
    }

    public static bool IsValidCompanyWebsite(string? website)
    {
        if (string.IsNullOrWhiteSpace(website))
        {
            return true;
        }

        var trimmed = website.Trim();

        if (trimmed.Length > CompanyWebsiteMaxLength)
        {
            return false;
        }

        if (!Uri.TryCreate(
                trimmed,
                UriKind.Absolute,
                out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp ||
               uri.Scheme == Uri.UriSchemeHttps;
    }
}