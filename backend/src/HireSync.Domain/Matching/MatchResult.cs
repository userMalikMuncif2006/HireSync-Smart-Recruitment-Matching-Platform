namespace HireSync.Domain.Matching;

public sealed record MatchResult(
    decimal TotalScore,
    decimal SkillsScore,
    decimal ExperienceScore,
    decimal EducationScore,
    decimal LocationScore,
    IReadOnlyList<MatchSkill> MatchedSkills,
    IReadOnlyList<MatchSkill> MissingSkills);
