using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.ContactRequests;

public sealed record RespondContactRequestRequest(
    ContactRequestStatus Status,
    byte[] RowVersion);
