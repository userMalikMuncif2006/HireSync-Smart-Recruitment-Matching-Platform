using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Admin;

public sealed record UpdateAdminAccountStatusRequest(
    AccountStatus Status,
    byte[] RowVersion);