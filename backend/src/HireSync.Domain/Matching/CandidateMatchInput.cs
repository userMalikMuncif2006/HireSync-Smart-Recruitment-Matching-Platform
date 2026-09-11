using HireSync.Domain.Entities;
using HireSync.Domain.Enums;

namespace HireSync.Domain.Matching;

public sealed class CandidateMatchInput
{
    public IReadOnlyCollection<MatchSkill> Skills { get; }

    public int TotalExperienceMonths { get; }

    public EducationLevel EducationLevel { get; }

    public string NormalizedPreferredLocation { get; }

    public CandidateMatchInput(
        IEnumerable<MatchSkill> skills,
        int totalExperienceMonths,
        EducationLevel educationLevel,
        string normalizedPreferredLocation)
    {
        ArgumentNullException.ThrowIfNull(skills);

        var materializedSkills = skills.ToArray();

        if (materializedSkills.Length == 0)
        {
            throw new ArgumentException(
                "A match-ready candidate must contain at least one skill.",
                nameof(skills));
        }

        if (totalExperienceMonths is < 0
            or > JobSeekerProfile.MaxExperienceMonths)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalExperienceMonths));
        }

        if (!Enum.IsDefined(typeof(EducationLevel), educationLevel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(educationLevel));
        }

        if (string.IsNullOrWhiteSpace(normalizedPreferredLocation))
        {
            throw new ArgumentException(
                "Normalized preferred location is required.",
                nameof(normalizedPreferredLocation));
        }

        Skills = materializedSkills;
        TotalExperienceMonths = totalExperienceMonths;
        EducationLevel = educationLevel;
        NormalizedPreferredLocation = normalizedPreferredLocation;
    }
}
