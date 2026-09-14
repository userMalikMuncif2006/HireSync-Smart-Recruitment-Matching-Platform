namespace HireSync.Application.DTOs.Admin;

public sealed record AdminUserListDto(
    IReadOnlyList<AdminUserListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);