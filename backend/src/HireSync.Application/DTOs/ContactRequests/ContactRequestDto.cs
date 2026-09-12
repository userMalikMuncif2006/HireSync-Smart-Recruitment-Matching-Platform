using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.ContactRequests;

public sealed record ContactRequestDto(
    Guid Id,
    Guid JobApplicationId,
    ContactRequestStatus Status,
    DateTime RequestedAtUtc,
    DateTime? RespondedAtUtc,
    byte[] RowVersion);
