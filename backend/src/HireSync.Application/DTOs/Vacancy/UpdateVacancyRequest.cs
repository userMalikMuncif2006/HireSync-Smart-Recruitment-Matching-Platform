using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Vacancy;

public sealed record UpdateVacancyRequest(
    string Title,
    string Description,
    string Location,
    int MinimumExperienceMonths,
    EducationLevel? RequiredEducationLevel,
    IReadOnlyList<Guid> RequiredSkillIds,
    byte[] RowVersion);