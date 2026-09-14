using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = RoleNames.Administrator)]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(
        IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(AdminDashboardDto),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboardDto>>
        GetDashboard(
            CancellationToken cancellationToken)
    {
        var dashboard =
            await _dashboardService.GetAsync(
                cancellationToken);

        return Ok(dashboard);
    }
}