using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Admin;

public sealed class AdminUserService : IAdminUserService
{
    private readonly HireSyncDbContext _dbContext;

    public AdminUserService(
        HireSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminUserListDto> GetUsersAsync(
        string? search,
        AccountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query =
            from user in _dbContext.Users.AsNoTracking()
            join userRole in _dbContext.UserRoles.AsNoTracking()
                on user.Id equals userRole.UserId
            join role in _dbContext.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            select new
            {
                user.Id,
                user.DisplayName,
                user.Email,
                user.NormalizedEmail,
                Role = role.Name,
                user.AccountStatus,
                user.CreatedAtUtc,
                user.RowVersion
            };

        if (status.HasValue)
        {
            query =
                query.Where(
                    item =>
                        item.AccountStatus == status.Value);
        }

        var trimmedSearch =
            search?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedSearch))
        {
            var normalizedSearch =
                trimmedSearch.ToUpperInvariant();

            query =
                query.Where(
                    item =>
                        item.DisplayName
                            .ToUpper()
                            .Contains(normalizedSearch) ||
                        (
                            item.NormalizedEmail != null &&
                            item.NormalizedEmail
                                .Contains(normalizedSearch)
                        ));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var rows =
            await query
                .OrderBy(item => item.DisplayName)
                .ThenBy(item => item.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        var items =
            rows
                .Select(
                    item =>
                        new AdminUserListItemDto(
                            item.Id,
                            item.DisplayName,
                            item.Email ?? string.Empty,
                            item.Role ?? string.Empty,
                            item.AccountStatus,
                            item.CreatedAtUtc,
                            Convert.ToBase64String(
                                item.RowVersion)))
                .ToArray();

        return new AdminUserListDto(
            items,
            page,
            pageSize,
            totalCount);
    }
}