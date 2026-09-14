using HireSync.Application.DTOs.Admin;
using HireSync.Domain.Enums;

namespace HireSync.Application.Interfaces.Admin;

public interface IAdminUserService
{
    Task<AdminUserListDto> GetUsersAsync(
        string? search,
        AccountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}