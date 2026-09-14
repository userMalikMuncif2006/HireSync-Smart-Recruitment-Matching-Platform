using HireSync.Application.DTOs;
using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Vacancy;

public sealed record VacancyDto(
    Guid Id,
    string Title,
    string Description,
    string Location,
    int MinimumExperienceMonths,
    EducationLevel? RequiredEducationLevel,
    VacancyStatus Status,
    DateTime PublishedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ClosedAtUtc,
    IReadOnlyList<SkillSummaryDto> RequiredSkills,
    byte[] RowVersion);