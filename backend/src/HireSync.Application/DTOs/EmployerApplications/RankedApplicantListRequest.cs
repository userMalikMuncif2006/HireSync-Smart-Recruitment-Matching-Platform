using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.EmployerApplications;

public sealed record RankedApplicantListRequest(
    ApplicationStatus? Status = null,
    int Page = 1,
    int PageSize = 20);