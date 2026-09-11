namespace HireSync.Domain.Entities;

public sealed class EmployerProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string NormalizedCompanyName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string NormalizedLocation { get; set; } = string.Empty;

    public string ContactPersonName { get; set; } = string.Empty;

    public string ContactPersonDesignation { get; set; } = string.Empty;

    public string BusinessRegistrationNumber { get; set; } = string.Empty;

    public string NormalizedBusinessRegistrationNumber { get; set; } =
        string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public string? CompanyWebsite { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsProfileComplete =>
        !string.IsNullOrWhiteSpace(CompanyName) &&
        !string.IsNullOrWhiteSpace(Description) &&
        !string.IsNullOrWhiteSpace(Location) &&
        !string.IsNullOrWhiteSpace(ContactPersonName) &&
        !string.IsNullOrWhiteSpace(ContactPersonDesignation) &&
        !string.IsNullOrWhiteSpace(BusinessRegistrationNumber) &&
        !string.IsNullOrWhiteSpace(MobileNumber);
}