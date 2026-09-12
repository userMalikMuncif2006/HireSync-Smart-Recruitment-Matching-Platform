using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.Persistence;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Application.Services;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Application.Tests;

public sealed class JobSeekerProfileServiceTests
{
    [Fact]
    public async Task UpdateOwnProfileAsync_CreatesProfileAndCanonicalSkillLinks()
    {
        await using var dbContext =
            CreateDbContext();

        var userId = Guid.NewGuid();

        var csharpSkill =
            new Skill(
                Guid.NewGuid(),
                "C#");

        var sqlSkill =
            new Skill(
                Guid.NewGuid(),
                "SQL");

        dbContext.Skills.AddRange(
            csharpSkill,
            sqlSkill);

        await dbContext.SaveChangesAsync();

        var service =
            CreateService(
                dbContext,
                userId);

        var request =
            new UpdateJobSeekerProfileRequest(
                24,
                EducationLevel.Bachelor,
                " Colombo ",
                new[]
                {
                    sqlSkill.Id,
                    csharpSkill.Id,
                    csharpSkill.Id
                });

        var result =
            await service.UpdateOwnProfileAsync(
                request);

        Assert.True(result.Succeeded);

        Assert.Equal(
            JobSeekerProfileUpdateFailureReason.None,
            result.FailureReason);

        Assert.NotNull(result.Profile);

        Assert.True(
            result.Profile!.IsMatchReady);

        Assert.Equal(
            2,
            result.Profile.Skills.Count);

        var profile =
            await dbContext.JobSeekerProfiles
                .SingleAsync();

        Assert.Equal(
            userId,
            profile.UserId);

        Assert.Equal(
            24,
            profile.TotalExperienceMonths);

        Assert.Equal(
            EducationLevel.Bachelor,
            profile.EducationLevel);

        Assert.False(
            string.IsNullOrWhiteSpace(
                profile.NormalizedPreferredLocation));

        var links =
            await dbContext.JobSeekerSkills
                .Where(
                    candidate =>
                        candidate.JobSeekerProfileId ==
                        profile.Id)
                .ToListAsync();

        Assert.Equal(
            2,
            links.Count);

        Assert.Contains(
            links,
            candidate =>
                candidate.SkillId ==
                csharpSkill.Id);

        Assert.Contains(
            links,
            candidate =>
                candidate.SkillId ==
                sqlSkill.Id);
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_RejectsUnknownSkill()
    {
        await using var dbContext =
            CreateDbContext();

        var service =
            CreateService(
                dbContext,
                Guid.NewGuid());

        var request =
            new UpdateJobSeekerProfileRequest(
                12,
                EducationLevel.Diploma,
                "Kandy",
                new[]
                {
                    Guid.NewGuid()
                });

        var result =
            await service.UpdateOwnProfileAsync(
                request);

        Assert.False(result.Succeeded);

        Assert.Equal(
            JobSeekerProfileUpdateFailureReason
                .SkillNotFound,
            result.FailureReason);

        Assert.Empty(
            await dbContext.JobSeekerProfiles
                .ToListAsync());
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_RejectsInvalidCurrentUser()
    {
        await using var dbContext =
            CreateDbContext();

        var currentUser =
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = Guid.NewGuid(),
                Role = RoleNames.Employer
            };

        var service =
            new JobSeekerProfileService(
                dbContext,
                currentUser,
                new FakeClock(
                    new DateTime(
                        2026,
                        9,
                        12,
                        5,
                        30,
                        0,
                        DateTimeKind.Utc)));

        var request =
            new UpdateJobSeekerProfileRequest(
                6,
                EducationLevel.Certificate,
                "Jaffna",
                Array.Empty<Guid>());

        var result =
            await service.UpdateOwnProfileAsync(
                request);

        Assert.False(result.Succeeded);

        Assert.Equal(
            JobSeekerProfileUpdateFailureReason
                .InvalidAuthenticatedUser,
            result.FailureReason);

        Assert.Empty(
            await dbContext.JobSeekerProfiles
                .ToListAsync());
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_ReplacesOnlyChangedSkillLinks()
    {
        await using var dbContext =
            CreateDbContext();

        var userId = Guid.NewGuid();

        var firstSkill =
            new Skill(
                Guid.NewGuid(),
                "Angular");

        var retainedSkill =
            new Skill(
                Guid.NewGuid(),
                "TypeScript");

        var replacementSkill =
            new Skill(
                Guid.NewGuid(),
                "SQL");

        dbContext.Skills.AddRange(
            firstSkill,
            retainedSkill,
            replacementSkill);

        await dbContext.SaveChangesAsync();

        var service =
            CreateService(
                dbContext,
                userId);

        var firstResult =
            await service.UpdateOwnProfileAsync(
                new UpdateJobSeekerProfileRequest(
                    18,
                    EducationLevel.Bachelor,
                    "Colombo",
                    new[]
                    {
                        firstSkill.Id,
                        retainedSkill.Id
                    }));

        Assert.True(
            firstResult.Succeeded);

        var secondResult =
            await service.UpdateOwnProfileAsync(
                new UpdateJobSeekerProfileRequest(
                    24,
                    EducationLevel.Bachelor,
                    "Colombo",
                    new[]
                    {
                        retainedSkill.Id,
                        replacementSkill.Id
                    }));

        Assert.True(
            secondResult.Succeeded);

        var profile =
            await dbContext.JobSeekerProfiles
                .SingleAsync();

        var links =
            await dbContext.JobSeekerSkills
                .Where(
                    candidate =>
                        candidate.JobSeekerProfileId ==
                        profile.Id)
                .ToListAsync();

        Assert.Equal(
            2,
            links.Count);

        Assert.DoesNotContain(
            links,
            candidate =>
                candidate.SkillId ==
                firstSkill.Id);

        Assert.Contains(
            links,
            candidate =>
                candidate.SkillId ==
                retainedSkill.Id);

        Assert.Contains(
            links,
            candidate =>
                candidate.SkillId ==
                replacementSkill.Id);
    }

    [Fact]
    public async Task GetOwnProfileAsync_ReturnsDerivedReadiness()
    {
        await using var dbContext =
            CreateDbContext();

        var userId = Guid.NewGuid();

        var skill =
            new Skill(
                Guid.NewGuid(),
                ".NET");

        dbContext.Skills.Add(skill);

        await dbContext.SaveChangesAsync();

        var service =
            CreateService(
                dbContext,
                userId);

        var updateResult =
            await service.UpdateOwnProfileAsync(
                new UpdateJobSeekerProfileRequest(
                    36,
                    EducationLevel.Master,
                    "Colombo",
                    new[]
                    {
                        skill.Id
                    }));

        Assert.True(
            updateResult.Succeeded);

        var profile =
            await service.GetOwnProfileAsync();

        Assert.NotNull(profile);

        Assert.True(
            profile!.IsMatchReady);

        Assert.Single(
            profile.Skills);

        Assert.Equal(
            skill.Id,
            profile.Skills[0].Id);
    }

    [Fact]
    public async Task GetOwnProfileAsync_ReturnsNullForDifferentRole()
    {
        await using var dbContext =
            CreateDbContext();

        var currentUser =
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = Guid.NewGuid(),
                Role = RoleNames.Administrator
            };

        var service =
            new JobSeekerProfileService(
                dbContext,
                currentUser,
                new FakeClock(
                    new DateTime(
                        2026,
                        9,
                        12,
                        5,
                        30,
                        0,
                        DateTimeKind.Utc)));

        var result =
            await service.GetOwnProfileAsync();

        Assert.Null(result);
    }

    private static JobSeekerProfileService CreateService(
        TestHireSyncDbContext dbContext,
        Guid userId)
    {
        return new JobSeekerProfileService(
            dbContext,
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = userId,
                Role = RoleNames.JobSeeker
            },
            new FakeClock(
                new DateTime(
                    2026,
                    9,
                    12,
                    5,
                    30,
                    0,
                    DateTimeKind.Utc)));
    }

    private static TestHireSyncDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<
                TestHireSyncDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid()
                        .ToString())
                .Options;

        return new TestHireSyncDbContext(
            options);
    }

    private sealed class TestHireSyncDbContext
        : DbContext,
          IHireSyncDbContext
    {
        public TestHireSyncDbContext(
            DbContextOptions<
                TestHireSyncDbContext> options)
            : base(options)
        {
        }

        public DbSet<JobSeekerProfile>
            JobSeekerProfiles =>
                Set<JobSeekerProfile>();

        public DbSet<Skill>
            Skills =>
                Set<Skill>();

        public DbSet<JobSeekerSkill>
            JobSeekerSkills =>
                Set<JobSeekerSkill>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<JobSeekerProfile>()
                .HasKey(
                    profile =>
                        profile.Id);

            modelBuilder
                .Entity<JobSeekerProfile>()
                .HasIndex(
                    profile =>
                        profile.UserId)
                .IsUnique();

            modelBuilder
                .Entity<Skill>()
                .HasKey(
                    skill =>
                        skill.Id);

            modelBuilder
                .Entity<JobSeekerSkill>()
                .HasKey(
                    link =>
                        new
                        {
                            link.JobSeekerProfileId,
                            link.SkillId
                        });
        }
    }

    private sealed class FakeCurrentUser
        : ICurrentUser
    {
        public bool IsAuthenticated
        {
            get;
            init;
        }

        public Guid? UserId
        {
            get;
            init;
        }

        public string? Role
        {
            get;
            init;
        }

        public string? Email
        {
            get;
            init;
        }
    }

    private sealed class FakeClock
        : IClock
    {
        public FakeClock(
            DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow
        {
            get;
        }
    }
}
