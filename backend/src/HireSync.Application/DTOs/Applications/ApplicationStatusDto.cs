using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Applications;

public sealed record ApplicationStatusDto(
    Guid Id,
    ApplicationStatus Status,
    DateTime UpdatedAtUtc,
    byte[] RowVersion);
