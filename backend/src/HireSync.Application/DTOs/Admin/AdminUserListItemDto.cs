using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Admin;

public sealed record AdminUserListItemDto(
    Guid Id,
    string DisplayName,
    string Email,
    string Role,
    AccountStatus AccountStatus,
    DateTime CreatedAtUtc,
    string RowVersion);