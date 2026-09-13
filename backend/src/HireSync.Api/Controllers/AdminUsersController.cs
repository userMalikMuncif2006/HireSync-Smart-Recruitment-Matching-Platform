using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = RoleNames.Administrator)]
public sealed class AdminUsersController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;
    private const int MaximumSearchLength = 100;

    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(
        IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(AdminUserListDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminUserListDto>>
        GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] AccountStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize,
            CancellationToken cancellationToken = default)
    {
        var trimmedSearch =
            search?.Trim();

        if (trimmedSearch?.Length > MaximumSearchLength)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid user search",
                detail:
                    $"Search must be {MaximumSearchLength} characters or fewer.");
        }

        if (status.HasValue &&
            !Enum.IsDefined(
                typeof(AccountStatus),
                status.Value))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid account status",
                detail:
                    "Account status must be Active or Suspended.");
        }

        if (page < 1)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid page",
                detail:
                    "Page must be greater than or equal to 1.");
        }

        if (pageSize < 1 ||
            pageSize > MaximumPageSize)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid page size",
                detail:
                    $"Page size must be between 1 and {MaximumPageSize}.");
        }

        var result =
            await _adminUserService.GetUsersAsync(
                trimmedSearch,
                status,
                page,
                pageSize,
                cancellationToken);

        return Ok(result);
    }
}