using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Vacancy;

public sealed record CreateVacancyRequest(
    string Title,
    string Description,
    string Location,
    int MinimumExperienceMonths,
    EducationLevel? RequiredEducationLevel,
    IReadOnlyList<Guid> RequiredSkillIds);