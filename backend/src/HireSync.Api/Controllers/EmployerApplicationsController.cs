using HireSync.Application.DTOs.Applications;
using HireSync.Application.Interfaces.Applications;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/employer/applications")]
[Authorize(Roles = RoleNames.Employer)]
public sealed class EmployerApplicationsController
    : ControllerBase
{
    private readonly IApplicationStatusService
        _applicationStatusService;

    private readonly ICurrentUser _currentUser;

    public EmployerApplicationsController(
        IApplicationStatusService applicationStatusService,
        ICurrentUser currentUser)
    {
        _applicationStatusService =
            applicationStatusService;

        _currentUser =
            currentUser;
    }

    [HttpPatch("{applicationId:guid}/status")]
    [ProducesResponseType(
        typeof(ApplicationStatusDto),
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
    public async Task<ActionResult<ApplicationStatusDto>>
        UpdateApplicationStatus(
            [FromRoute] Guid applicationId,
            [FromBody] UpdateApplicationStatusRequest request,
            CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated ||
            !_currentUser.UserId.HasValue ||
            _currentUser.UserId.Value == Guid.Empty)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title:
                    "Invalid authenticated user",
                detail:
                    "The authenticated Employer identifier is invalid.");
        }

        var result =
            await _applicationStatusService
                .UpdateOwnApplicationStatusAsync(
                    _currentUser.UserId.Value,
                    applicationId,
                    request,
                    cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Application);
        }

        return result.FailureReason switch
        {
            ApplicationStatusUpdateFailureReason.InvalidInput =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid application status update",
                    detail:
                        "The submitted application status update is invalid."),

            ApplicationStatusUpdateFailureReason.NotFound =>
                Problem(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title:
                        "Application not found",
                    detail:
                        "The application could not be found."),

            ApplicationStatusUpdateFailureReason.InvalidTransition =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid application status transition",
                    detail:
                        "The requested application status transition is not allowed."),

            ApplicationStatusUpdateFailureReason.ConcurrencyConflict =>
                Problem(
                    statusCode:
                        StatusCodes.Status409Conflict,
                    title:
                        "Application was modified",
                    detail:
                        "The application has changed since it was loaded. Reload it and try again."),

            ApplicationStatusUpdateFailureReason.PersistenceFailed =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Application status update failed",
                    detail:
                        "The application status could not be saved."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Application status update failed")
        };
    }
}
