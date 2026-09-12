using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.Skills;
using HireSync.Domain.Rules;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Skills;

public sealed class SkillLookupService
    : ISkillLookupService
{
    private readonly HireSyncDbContext _dbContext;

    public SkillLookupService(
        HireSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SkillSummaryDto>>
        GetSkillsAsync(
            string? query,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var skills =
            _dbContext.Skills
                .AsNoTracking()
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            string normalizedQuery;

            try
            {
                normalizedQuery =
                    SkillNameNormalizer.Normalize(
                        query);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(
                    "Skill lookup query is invalid.",
                    nameof(query),
                    exception);
            }

            skills =
                skills.Where(
                    skill =>
                        skill.NormalizedName.Contains(
                            normalizedQuery));
        }

        return await skills
            .OrderBy(
                skill =>
                    skill.NormalizedName)
            .ThenBy(
                skill =>
                    skill.Id)
            .Select(
                skill =>
                    new SkillSummaryDto(
                        skill.Id,
                        skill.Name))
            .ToArrayAsync(
                cancellationToken);
    }
}
