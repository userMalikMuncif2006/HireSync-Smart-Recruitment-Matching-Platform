namespace HireSync.Application.DTOs.Applications;

public sealed record JobSeekerApplicationPageDto(
    IReadOnlyList<JobSeekerApplicationListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
