using HireSync.Application.DTOs;

namespace HireSync.Application.DTOs.Matching;

public sealed record MatchResultDto(
    decimal TotalScore,
    decimal SkillsScore,
    decimal ExperienceScore,
    decimal EducationScore,
    decimal LocationScore,
    IReadOnlyList<SkillSummaryDto> MatchedSkills,
    IReadOnlyList<SkillSummaryDto> MissingSkills);
