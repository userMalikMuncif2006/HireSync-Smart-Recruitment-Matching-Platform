using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Applications;

public sealed record JobSeekerApplicationListRequest(
    ApplicationStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
