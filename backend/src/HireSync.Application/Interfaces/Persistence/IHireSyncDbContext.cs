using HireSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Application.Interfaces.Persistence;

public interface IHireSyncDbContext
{
    DbSet<JobSeekerProfile> JobSeekerProfiles { get; }

    DbSet<Skill> Skills { get; }

    DbSet<JobSeekerSkill> JobSeekerSkills { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
