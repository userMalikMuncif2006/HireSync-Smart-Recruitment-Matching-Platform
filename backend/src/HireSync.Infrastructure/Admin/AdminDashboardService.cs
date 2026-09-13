using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Interfaces.Time;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Admin;

public sealed class AdminDashboardService
    : IAdminDashboardService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly IClock _clock;

    public AdminDashboardService(
        HireSyncDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<AdminDashboardDto> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var totalUsers =
            await _dbContext.Users
                .AsNoTracking()
                .CountAsync(cancellationToken);

        var totalVacancies =
            await _dbContext.Vacancies
                .AsNoTracking()
                .CountAsync(cancellationToken);

        var totalApplications =
            await _dbContext.JobApplications
                .AsNoTracking()
                .CountAsync(cancellationToken);

        return new AdminDashboardDto(
            totalUsers,
            totalVacancies,
            totalApplications,
            _clock.UtcNow);
    }
}