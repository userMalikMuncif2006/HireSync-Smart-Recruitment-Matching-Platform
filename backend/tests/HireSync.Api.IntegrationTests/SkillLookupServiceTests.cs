using HireSync.Domain.Entities;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Skills;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Api.IntegrationTests;

public sealed class SkillLookupServiceTests
{
    [Fact]
    public async Task No_query_returns_all_skills_in_deterministic_order()
    {
        await using var context =
            await CreateContextAsync(
                new Skill(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000004"),
                    "TypeScript"),
                new Skill(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000002"),
                    "C#"),
                new Skill(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000001"),
                    "Angular"),
                new Skill(
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000003"),
                    "C++"));

        var service =
            new SkillLookupService(
                context);

        var result =
            await service.GetSkillsAsync(
                query:
                    null);

        Assert.Equal(
            new[]
            {
                "Angular",
                "C#",
                "C++",
                "TypeScript"
            },
            result.Select(
                skill =>
                    skill.Name));
    }

    [Fact]
    public async Task Query_uses_existing_canonical_normalization()
    {
        await using var context =
            await CreateContextAsync(
                new Skill(
                    Guid.NewGuid(),
                    "ASP.NET Core"),
                new Skill(
                    Guid.NewGuid(),
                    "Angular"),
                new Skill(
                    Guid.NewGuid(),
                    "TypeScript"));

        var service =
            new SkillLookupService(
                context);

        var result =
            await service.GetSkillsAsync(
                "  asp.net   core  ");

        var skill =
            Assert.Single(result);

        Assert.Equal(
            "ASP.NET Core",
            skill.Name);
    }

    [Fact]
    public async Task Query_preserves_punctuation_semantics()
    {
        await using var context =
            await CreateContextAsync(
                new Skill(
                    Guid.NewGuid(),
                    "C#"),
                new Skill(
                    Guid.NewGuid(),
                    "C++"));

        var service =
            new SkillLookupService(
                context);

        var result =
            await service.GetSkillsAsync(
                "c#");

        var skill =
            Assert.Single(result);

        Assert.Equal(
            "C#",
            skill.Name);
    }

    [Fact]
    public async Task Blank_query_is_equivalent_to_no_filter()
    {
        await using var context =
            await CreateContextAsync(
                new Skill(
                    Guid.NewGuid(),
                    "Angular"),
                new Skill(
                    Guid.NewGuid(),
                    "TypeScript"));

        var service =
            new SkillLookupService(
                context);

        var result =
            await service.GetSkillsAsync(
                "   ");

        Assert.Equal(
            2,
            result.Count);
    }

    [Fact]
    public async Task Query_over_canonical_maximum_is_rejected()
    {
        await using var context =
            await CreateContextAsync();

        var service =
            new SkillLookupService(
                context);

        await Assert.ThrowsAsync<
            ArgumentException>(
            () =>
                service.GetSkillsAsync(
                    new string(
                        'x',
                        51)));
    }

    [Fact]
    public async Task Read_lookup_does_not_track_skill_entities()
    {
        await using var context =
            await CreateContextAsync(
                new Skill(
                    Guid.NewGuid(),
                    "Angular"));

        var service =
            new SkillLookupService(
                context);

        _ =
            await service.GetSkillsAsync(
                query:
                    null);

        Assert.Empty(
            context.ChangeTracker
                .Entries<Skill>());
    }

    private static async Task<HireSyncDbContext>
        CreateContextAsync(
            params Skill[] skills)
    {
        var options =
            new DbContextOptionsBuilder<
                    HireSyncDbContext>()
                .UseInMemoryDatabase(
                    $"HireSync-SkillLookup-{Guid.NewGuid():N}")
                .Options;

        var context =
            new HireSyncDbContext(
                options);

        await context.Database
            .EnsureCreatedAsync();

        if (skills.Length > 0)
        {
            await context.Skills
                .AddRangeAsync(
                    skills);

            await context
                .SaveChangesAsync();
        }

        context.ChangeTracker.Clear();

        return context;
    }
}
