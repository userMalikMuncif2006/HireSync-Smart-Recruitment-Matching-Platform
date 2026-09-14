using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs;

public sealed record UpdateJobSeekerProfileRequest(
    int ExperienceMonths,
    EducationLevel EducationLevel,
    string PreferredLocation,
    IReadOnlyList<Guid> SkillIds);