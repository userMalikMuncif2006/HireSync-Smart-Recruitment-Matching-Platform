using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Entities;

public sealed class Vacancy
{
    public Guid Id { get; private set; }

    public Guid EmployerProfileId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Location { get; private set; } = string.Empty;

    public string NormalizedLocation { get; private set; } = string.Empty;

    public int MinimumExperienceMonths { get; private set; }

    public EducationLevel? RequiredEducationLevel { get; private set; }

    public VacancyStatus Status { get; private set; }

    public DateTime PublishedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private Vacancy()
    {
    }

    public Vacancy(
        Guid id,
        Guid employerProfileId,
        string title,
        string description,
        string location,
        int minimumExperienceMonths,
        EducationLevel? requiredEducationLevel,
        DateTime publishedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Vacancy ID cannot be empty.",
                nameof(id));
        }

        if (employerProfileId == Guid.Empty)
        {
            throw new ArgumentException(
                "Employer profile ID cannot be empty.",
                nameof(employerProfileId));
        }

        EnsureUtc(
            publishedAtUtc,
            nameof(publishedAtUtc));

        ValidateRequirements(
            title,
            description,
            location,
            minimumExperienceMonths,
            requiredEducationLevel);

        Id = id;
        EmployerProfileId = employerProfileId;

        ApplyRequirements(
            title,
            description,
            location,
            minimumExperienceMonths,
            requiredEducationLevel);

        Status = VacancyStatus.Open;
        PublishedAtUtc = publishedAtUtc;
        UpdatedAtUtc = publishedAtUtc;
        ClosedAtUtc = null;
    }

    public void UpdateRequirements(
        string title,
        string description,
        string location,
        int minimumExperienceMonths,
        EducationLevel? requiredEducationLevel,
        DateTime updatedAtUtc)
    {
        if (!VacancyRules.CanUpdate(Status))
        {
            throw new InvalidOperationException(
                "Only an Open vacancy can be updated.");
        }

        EnsureUtc(
            updatedAtUtc,
            nameof(updatedAtUtc));

        ValidateRequirements(
            title,
            description,
            location,
            minimumExperienceMonths,
            requiredEducationLevel);

        ApplyRequirements(
            title,
            description,
            location,
            minimumExperienceMonths,
            requiredEducationLevel);

        UpdatedAtUtc = updatedAtUtc;
    }

    public void Close(DateTime closedAtUtc)
    {
        if (!VacancyRules.CanClose(Status))
        {
            throw new InvalidOperationException(
                "Only an Open vacancy can be closed.");
        }

        EnsureUtc(
            closedAtUtc,
            nameof(closedAtUtc));

        Status = VacancyStatus.Closed;
        ClosedAtUtc = closedAtUtc;
        UpdatedAtUtc = closedAtUtc;
    }

    private void ApplyRequirements(
        string title,
        string description,
        string location,
        int minimumExperienceMonths,
        EducationLevel? requiredEducationLevel)
    {
        Title = title.Trim();
        Description = description.Trim();

        Location =
            LocationNormalizer.CanonicalizeDisplayLocation(location);

        NormalizedLocation =
            LocationNormalizer.Normalize(location);

        MinimumExperienceMonths =
            minimumExperienceMonths;

        RequiredEducationLevel =
            requiredEducationLevel;
    }

    private static void ValidateRequirements(
        string title,
        string description,
        string location,
        int minimumExperienceMonths,
        EducationLevel? requiredEducationLevel)
    {
        if (!VacancyRules.IsValidTitle(title))
        {
            throw new ArgumentException(
                $"Vacancy title must be between {VacancyRules.TitleMinLength} and {VacancyRules.TitleMaxLength} characters.",
                nameof(title));
        }

        if (!VacancyRules.IsValidDescription(description))
        {
            throw new ArgumentException(
                $"Vacancy description must be between {VacancyRules.DescriptionMinLength} and {VacancyRules.DescriptionMaxLength} characters.",
                nameof(description));
        }

        if (!VacancyRules.IsValidLocation(location))
        {
            throw new ArgumentException(
                $"Vacancy location must be between {VacancyRules.LocationMinLength} and {VacancyRules.LocationMaxLength} characters.",
                nameof(location));
        }

        if (!VacancyRules.IsValidMinimumExperienceMonths(
                minimumExperienceMonths))
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumExperienceMonths),
                $"Minimum experience must be between {VacancyRules.MinimumExperienceMonthsMin} and {VacancyRules.MinimumExperienceMonthsMax} months.");
        }

        if (requiredEducationLevel.HasValue &&
            !Enum.IsDefined(
                typeof(EducationLevel),
                requiredEducationLevel.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredEducationLevel),
                "Required education level is invalid.");
        }
    }

    private static void EnsureUtc(
        DateTime value,
        string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Timestamp must be UTC.",
                parameterName);
        }
    }
}