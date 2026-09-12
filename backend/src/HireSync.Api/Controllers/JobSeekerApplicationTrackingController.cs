using HireSync.Application.DTOs.Applications;
using HireSync.Application.Interfaces.Applications;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/job-seeker/applications")]
[Authorize(Roles = RoleNames.JobSeeker)]
public sealed class JobSeekerApplicationTrackingController
    : ControllerBase
{
    private readonly IJobSeekerApplicationTrackingService
        _trackingService;

    private readonly ICurrentUser
        _currentUser;

    public JobSeekerApplicationTrackingController(
        IJobSeekerApplicationTrackingService trackingService,
        ICurrentUser currentUser)
    {
        _trackingService =
            trackingService;

        _currentUser =
            currentUser;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(JobSeekerApplicationPageDto),
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
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobSeekerApplicationPageDto>>
        GetApplications(
            [FromQuery]
            JobSeekerApplicationListRequest request,
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
                    "The authenticated Job Seeker identifier is invalid.");
        }

        var result =
            await _trackingService
                .GetOwnApplicationsAsync(
                    _currentUser.UserId.Value,
                    request,
                    cancellationToken);

        if (result.Succeeded)
        {
            return Ok(
                result.Page);
        }

        return result.FailureReason switch
        {
            JobSeekerApplicationTrackingFailureReason.InvalidInput =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid application list request",
                    detail:
                        "The submitted application tracking parameters are invalid."),

            JobSeekerApplicationTrackingFailureReason.JobSeekerUnavailable =>
                Problem(
                    statusCode:
                        StatusCodes.Status403Forbidden,
                    title:
                        "Job Seeker unavailable",
                    detail:
                        "The authenticated Job Seeker cannot access application tracking."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Application tracking failed",
                    detail:
                        "The application tracking list could not be loaded.")
        };
    }
}
