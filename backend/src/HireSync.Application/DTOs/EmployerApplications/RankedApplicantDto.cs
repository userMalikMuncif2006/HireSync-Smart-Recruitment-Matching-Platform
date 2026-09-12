using HireSync.Application.DTOs.Matching;
using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.EmployerApplications;

public sealed record RankedApplicantDto(
    int Rank,
    Guid ApplicationId,
    Guid JobSeekerProfileId,
    string JobSeekerDisplayName,
    ApplicationStatus Status,
    DateTime AppliedAtUtc,
    DateTime UpdatedAtUtc,
    byte[] RowVersion,
    ContactRequestStatus? ContactRequestStatus,
    MatchResultDto Match);