using HireSync.Application.DTOs.Admin;

namespace HireSync.Application.Interfaces.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetAsync(
        CancellationToken cancellationToken = default);
}