namespace HireSync.Domain.Matching;

public sealed class MatchEngine : IMatchEngine
{
    private const decimal SkillsWeight = 50m;
    private const decimal ExperienceWeight = 25m;
    private const decimal EducationWeight = 15m;
    private const decimal LocationWeight = 10m;

    public MatchResult Calculate(
        CandidateMatchInput candidate,
        VacancyMatchInput vacancy)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(vacancy);

        var candidateSkills =
            CreateCanonicalSkillMap(candidate.Skills);

        var requiredSkills =
            CreateCanonicalSkillMap(vacancy.RequiredSkills);

        if (requiredSkills.Count == 0)
        {
            throw new InvalidOperationException(
                "A valid vacancy must contain at least one required skill.");
        }

        if (candidateSkills.Count == 0)
        {
            throw new InvalidOperationException(
                "A match-ready candidate must contain at least one skill.");
        }

        var matchedSkills = OrderSkills(
            requiredSkills
                .Where(pair => candidateSkills.ContainsKey(pair.Key))
                .Select(pair => pair.Value))
            .ToArray();

        var missingSkills = OrderSkills(
            requiredSkills
                .Where(pair => !candidateSkills.ContainsKey(pair.Key))
                .Select(pair => pair.Value))
            .ToArray();

        var rawSkillsScore =
            ((decimal)matchedSkills.Length / requiredSkills.Count)
            * SkillsWeight;

        var rawExperienceScore =
            vacancy.MinimumExperienceMonths == 0
                ? ExperienceWeight
                : Math.Min(
                    (decimal)candidate.TotalExperienceMonths
                    / vacancy.MinimumExperienceMonths,
                    1m)
                * ExperienceWeight;

        var rawEducationScore =
            !vacancy.RequiredEducationLevel.HasValue
            || candidate.EducationLevel
                >= vacancy.RequiredEducationLevel.Value
                ? EducationWeight
                : 0m;

        var rawLocationScore =
            string.Equals(
                candidate.NormalizedPreferredLocation,
                vacancy.NormalizedLocation,
                StringComparison.Ordinal)
                ? LocationWeight
                : 0m;

        var rawTotal =
            rawSkillsScore
            + rawExperienceScore
            + rawEducationScore
            + rawLocationScore;

        return new MatchResult(
            Round(rawTotal),
            Round(rawSkillsScore),
            Round(rawExperienceScore),
            Round(rawEducationScore),
            Round(rawLocationScore),
            matchedSkills,
            missingSkills);
    }

    private static Dictionary<Guid, MatchSkill>
        CreateCanonicalSkillMap(
            IEnumerable<MatchSkill> skills)
    {
        return skills
            .GroupBy(skill => skill.Id)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var canonical = group
                        .OrderBy(
                            skill => skill.NormalizedName,
                            StringComparer.Ordinal)
                        .ThenBy(
                            skill => skill.Name,
                            StringComparer.Ordinal)
                        .First();

                    if (group.Any(
                        skill =>
                            !string.Equals(
                                skill.Name,
                                canonical.Name,
                                StringComparison.Ordinal)
                            || !string.Equals(
                                skill.NormalizedName,
                                canonical.NormalizedName,
                                StringComparison.Ordinal)))
                    {
                        throw new InvalidOperationException(
                            $"Conflicting canonical skill data detected for skill '{group.Key}'.");
                    }

                    return canonical;
                });
    }

    private static IOrderedEnumerable<MatchSkill> OrderSkills(
        IEnumerable<MatchSkill> skills)
    {
        return skills
            .OrderBy(
                skill => skill.NormalizedName,
                StringComparer.Ordinal)
            .ThenBy(
                skill => skill.Id
                    .ToString("N")
                    .ToLowerInvariant(),
                StringComparer.Ordinal);
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }
}
