using HireSync.Application.Interfaces.Time;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Admin;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Api.IntegrationTests;

public sealed class AdminDashboardServiceTests
{
    [Fact]
    public async Task GetAsync_returns_authoritative_counts_and_clock_time()
    {
        var options =
            new DbContextOptionsBuilder<HireSyncDbContext>()
                .UseInMemoryDatabase(
                    $"admin-dashboard-{Guid.NewGuid()}")
                .Options;

        await using var dbContext =
            new HireSyncDbContext(options);

        var now =
            new DateTime(
                2026,
                9,
                13,
                3,
                30,
                0,
                DateTimeKind.Utc);

        dbContext.Users.AddRange(
            CreateUser(
                "active@example.com",
                AccountStatus.Active,
                now),
            CreateUser(
                "suspended@example.com",
                AccountStatus.Suspended,
                now));

        var vacancyOneId = Guid.NewGuid();
        var vacancyTwoId = Guid.NewGuid();

        dbContext.Vacancies.AddRange(
            new Vacancy(
                vacancyOneId,
                Guid.NewGuid(),
                "Angular Developer",
                "Build and maintain production Angular applications for HireSync.",
                "Colombo",
                0,
                null,
                now),
            new Vacancy(
                vacancyTwoId,
                Guid.NewGuid(),
                "Backend Developer",
                "Build and maintain ASP.NET Core backend services for HireSync.",
                "Kandy",
                0,
                null,
                now));

        dbContext.JobApplications.Add(
            new JobApplication(
                Guid.NewGuid(),
                vacancyOneId,
                Guid.NewGuid(),
                now));

        await dbContext.SaveChangesAsync();

        var service =
            new AdminDashboardService(
                dbContext,
                new FakeClock(now));

        var result =
            await service.GetAsync();

        Assert.Equal(2, result.TotalUsers);
        Assert.Equal(2, result.TotalVacancies);
        Assert.Equal(1, result.TotalApplications);
        Assert.Equal(now, result.CalculatedAtUtc);
    }

    [Fact]
    public async Task GetAsync_returns_zero_counts_for_empty_database()
    {
        var options =
            new DbContextOptionsBuilder<HireSyncDbContext>()
                .UseInMemoryDatabase(
                    $"admin-dashboard-empty-{Guid.NewGuid()}")
                .Options;

        await using var dbContext =
            new HireSyncDbContext(options);

        var now =
            new DateTime(
                2026,
                9,
                13,
                4,
                0,
                0,
                DateTimeKind.Utc);

        var service =
            new AdminDashboardService(
                dbContext,
                new FakeClock(now));

        var result =
            await service.GetAsync();

        Assert.Equal(0, result.TotalUsers);
        Assert.Equal(0, result.TotalVacancies);
        Assert.Equal(0, result.TotalApplications);
        Assert.Equal(now, result.CalculatedAtUtc);
    }

    private static ApplicationUser CreateUser(
        string email,
        AccountStatus accountStatus,
        DateTime now)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName =
                email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail =
                email.ToUpperInvariant(),
            DisplayName = "Dashboard Test User",
            AccountStatus = accountStatus,
            TokenVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}