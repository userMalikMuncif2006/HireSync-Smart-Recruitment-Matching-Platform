using HireSync.Application.DTOs;

namespace HireSync.Application.Interfaces.Skills;

public interface ISkillLookupService
{
    Task<IReadOnlyList<SkillSummaryDto>> GetSkillsAsync(
        string? query,
        CancellationToken cancellationToken = default);
}
