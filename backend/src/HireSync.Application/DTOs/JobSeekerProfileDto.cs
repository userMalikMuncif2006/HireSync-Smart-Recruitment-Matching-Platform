using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs;

public sealed record JobSeekerProfileDto(
    int? TotalExperienceMonths,
    EducationLevel? EducationLevel,
    string? PreferredLocation,
    IReadOnlyList<SkillSummaryDto> Skills,
    bool IsMatchReady);
