using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Vacancy;

public sealed record PublicVacancyListItemDto(
    Guid Id,
    string Title,
    string CompanyName,
    string Location,
    int MinimumExperienceMonths,
    EducationLevel? RequiredEducationLevel,
    DateTime PublishedAtUtc);