using HireSync.Application.DTOs.Matching;
using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Vacancy;

public sealed record PublicVacancyDetailDto(
    Guid Id,
    string Title,
    string Description,
    string CompanyName,
    string CompanyDescription,
    string? CompanyWebsite,
    string CompanyLocation,
    string Location,
    int MinimumExperienceMonths,
    EducationLevel? RequiredEducationLevel,
    DateTime PublishedAtUtc,
    IReadOnlyList<SkillSummaryDto> RequiredSkills,
    string MatchStatus,
    MatchResultDto? Match,
    IReadOnlyList<string> MissingProfileFields,
    bool CanApply,
    bool HasApplied,
    DateTime ComputedAtUtc);