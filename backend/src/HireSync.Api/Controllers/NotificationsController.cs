using HireSync.Application.DTOs.Notifications;
using HireSync.Application.Interfaces.Notifications;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize(Roles = RoleNames.JobSeeker)]
public sealed class NotificationsController
    : ControllerBase
{
    private readonly IJobSeekerNotificationService
        _notificationService;

    private readonly ICurrentUser
        _currentUser;

    public NotificationsController(
        IJobSeekerNotificationService notificationService,
        ICurrentUser currentUser)
    {
        _notificationService =
            notificationService;

        _currentUser =
            currentUser;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(NotificationPageDto),
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
    public async Task<ActionResult<NotificationPageDto>>
        GetNotifications(
            [FromQuery]
            NotificationListRequest request,
            CancellationToken cancellationToken)
    {
        var userIdResult =
            GetAuthenticatedUserId();

        if (!userIdResult.Succeeded)
        {
            return UnauthorizedProblem();
        }

        var result =
            await _notificationService
                .GetOwnNotificationsAsync(
                    userIdResult.UserId,
                    request,
                    cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Page);
        }

        return ReadProblem(
            result.FailureReason);
    }

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
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
    public async Task<IActionResult>
        MarkRead(
            [FromRoute]
            Guid notificationId,
            CancellationToken cancellationToken)
    {
        var userIdResult =
            GetAuthenticatedUserId();

        if (!userIdResult.Succeeded)
        {
            return UnauthorizedProblem();
        }

        var result =
            await _notificationService
                .MarkReadAsync(
                    userIdResult.UserId,
                    notificationId,
                    cancellationToken);

        if (result.Succeeded)
        {
            return NoContent();
        }

        return ReadProblem(
            result.FailureReason);
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult>
        MarkAllRead(
            CancellationToken cancellationToken)
    {
        var userIdResult =
            GetAuthenticatedUserId();

        if (!userIdResult.Succeeded)
        {
            return UnauthorizedProblem();
        }

        var result =
            await _notificationService
                .MarkAllReadAsync(
                    userIdResult.UserId,
                    cancellationToken);

        if (result.Succeeded)
        {
            return NoContent();
        }

        return ReadProblem(
            result.FailureReason);
    }

    private (bool Succeeded, Guid UserId)
        GetAuthenticatedUserId()
    {
        if (!_currentUser.IsAuthenticated ||
            !_currentUser.UserId.HasValue ||
            _currentUser.UserId.Value == Guid.Empty)
        {
            return (
                false,
                Guid.Empty);
        }

        return (
            true,
            _currentUser.UserId.Value);
    }

    private ObjectResult ReadProblem(
        NotificationReadFailureReason reason)
    {
        return reason switch
        {
            NotificationReadFailureReason.InvalidInput =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid notification request",
                    detail:
                        "The submitted notification request is invalid."),

            NotificationReadFailureReason.JobSeekerUnavailable =>
                Problem(
                    statusCode:
                        StatusCodes.Status403Forbidden,
                    title:
                        "Job Seeker unavailable",
                    detail:
                        "The authenticated Job Seeker cannot access notifications."),

            NotificationReadFailureReason.NotificationNotFound =>
                Problem(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title:
                        "Notification not found",
                    detail:
                        "The notification could not be found."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Notification operation failed",
                    detail:
                        "The notification operation could not be completed.")
        };
    }

    private ObjectResult UnauthorizedProblem()
    {
        return Problem(
            statusCode:
                StatusCodes.Status401Unauthorized,
            title:
                "Invalid authenticated user",
            detail:
                "The authenticated Job Seeker identifier is invalid.");
    }
}
