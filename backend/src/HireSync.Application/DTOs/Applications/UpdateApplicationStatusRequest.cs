using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Applications;

public sealed record UpdateApplicationStatusRequest(
    ApplicationStatus Status,
    byte[] RowVersion);
