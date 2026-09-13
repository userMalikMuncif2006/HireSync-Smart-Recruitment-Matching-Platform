using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = RoleNames.Administrator)]
public sealed class AdminAccountStatusController
    : ControllerBase
{
    private readonly IAdminAccountStatusService
        _statusService;

    private readonly ICurrentUser
        _currentUser;

    public AdminAccountStatusController(
        IAdminAccountStatusService statusService,
        ICurrentUser currentUser)
    {
        _statusService = statusService;
        _currentUser = currentUser;
    }

    [HttpPatch("{userId:guid}/status")]
    [ProducesResponseType(
        typeof(AdminUserListItemDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AdminUserListItemDto>>
        UpdateStatus(
            Guid userId,
            UpdateAdminAccountStatusRequest request,
            CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated ||
            !_currentUser.UserId.HasValue)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title: "Authentication required",
                detail:
                    "A valid Administrator session is required.");
        }

        var result =
            await _statusService.UpdateStatusAsync(
                _currentUser.UserId.Value,
                userId,
                request,
                cancellationToken);

        if (result.Succeeded &&
            result.User is not null)
        {
            return Ok(result.User);
        }

        return result.FailureReason switch
        {
            AdminAccountStatusUpdateFailureReason
                .InvalidInput =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title: "Invalid account status update",
                    detail:
                        "The requested account status update is invalid."),

            AdminAccountStatusUpdateFailureReason
                .NotFound =>
                Problem(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title: "User not found",
                    detail:
                        "The requested user account could not be found."),

            AdminAccountStatusUpdateFailureReason
                .ProtectedTarget =>
                Problem(
                    statusCode:
                        StatusCodes.Status403Forbidden,
                    title: "Protected account",
                    detail:
                        "Administrator accounts and the current Administrator account cannot be changed through this operation."),

            AdminAccountStatusUpdateFailureReason
                .ConcurrencyConflict =>
                Problem(
                    statusCode:
                        StatusCodes.Status409Conflict,
                    title: "Account changed",
                    detail:
                        "The account was changed by another operation. Refresh and try again."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title: "Account update failed",
                    detail:
                        "The account status could not be persisted.")
        };
    }
}