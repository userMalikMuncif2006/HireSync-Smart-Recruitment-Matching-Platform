using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Matching;

public sealed class VacancyMatchInput
{
    public IReadOnlyCollection<MatchSkill> RequiredSkills { get; }

    public int MinimumExperienceMonths { get; }

    public EducationLevel? RequiredEducationLevel { get; }

    public string NormalizedLocation { get; }

    public VacancyMatchInput(
        IEnumerable<MatchSkill> requiredSkills,
        int minimumExperienceMonths,
        EducationLevel? requiredEducationLevel,
        string normalizedLocation)
    {
        ArgumentNullException.ThrowIfNull(requiredSkills);

        var materializedSkills = requiredSkills.ToArray();

        if (materializedSkills.Length == 0)
        {
            throw new ArgumentException(
                "A vacancy must contain at least one required skill.",
                nameof(requiredSkills));
        }

        if (!VacancyRules.IsValidMinimumExperienceMonths(
                minimumExperienceMonths))
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumExperienceMonths));
        }

        if (requiredEducationLevel.HasValue
            && !Enum.IsDefined(
                typeof(EducationLevel),
                requiredEducationLevel.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredEducationLevel));
        }

        if (string.IsNullOrWhiteSpace(normalizedLocation))
        {
            throw new ArgumentException(
                "Normalized vacancy location is required.",
                nameof(normalizedLocation));
        }

        RequiredSkills = materializedSkills;
        MinimumExperienceMonths = minimumExperienceMonths;
        RequiredEducationLevel = requiredEducationLevel;
        NormalizedLocation = normalizedLocation;
    }
}
