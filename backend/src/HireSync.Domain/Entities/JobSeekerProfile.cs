using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Entities;

public sealed class JobSeekerProfile
{
    public const int MaxExperienceMonths = 720;

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public int? TotalExperienceMonths { get; private set; }

    public EducationLevel? EducationLevel { get; private set; }

    public string? PreferredLocation { get; private set; }

    public string? NormalizedPreferredLocation { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private JobSeekerProfile()
    {
    }

    public JobSeekerProfile(
        Guid id,
        Guid userId,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Job Seeker profile ID cannot be empty.",
                nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Job Seeker user ID cannot be empty.",
                nameof(userId));
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public void UpdateStructuredProfile(
        int totalExperienceMonths,
        EducationLevel educationLevel,
        string preferredLocation,
        DateTime updatedAtUtc)
    {
        if (totalExperienceMonths is < 0 or > MaxExperienceMonths)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalExperienceMonths),
                $"Total experience must be between 0 and {MaxExperienceMonths} months.");
        }

        if (!Enum.IsDefined(typeof(EducationLevel), educationLevel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(educationLevel),
                "Education level is invalid.");
        }

        if (string.IsNullOrWhiteSpace(preferredLocation))
        {
            throw new ArgumentException(
                "Preferred location is required.",
                nameof(preferredLocation));
        }

        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        var displayLocation =
            LocationNormalizer.CanonicalizeDisplayLocation(preferredLocation);

        var normalizedLocation =
            LocationNormalizer.Normalize(preferredLocation);

        if (string.IsNullOrWhiteSpace(normalizedLocation))
        {
            throw new ArgumentException(
                "Preferred location is required.",
                nameof(preferredLocation));
        }

        TotalExperienceMonths = totalExperienceMonths;
        EducationLevel = educationLevel;
        PreferredLocation = displayLocation;
        NormalizedPreferredLocation = normalizedLocation;
        UpdatedAtUtc = updatedAtUtc;
    }

    public bool IsMatchReady(bool hasAtLeastOneSkill)
    {
        return TotalExperienceMonths.HasValue
            && EducationLevel.HasValue
            && !string.IsNullOrWhiteSpace(NormalizedPreferredLocation)
            && hasAtLeastOneSkill;
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