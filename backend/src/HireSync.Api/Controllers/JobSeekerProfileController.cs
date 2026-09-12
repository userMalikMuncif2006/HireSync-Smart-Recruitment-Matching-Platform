using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.JobSeeker;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/job-seeker/profile")]
[Authorize(Roles = RoleNames.JobSeeker)]
public sealed class JobSeekerProfileController : ControllerBase
{
    private readonly IJobSeekerProfileService _jobSeekerProfileService;

    public JobSeekerProfileController(
        IJobSeekerProfileService jobSeekerProfileService)
    {
        _jobSeekerProfileService = jobSeekerProfileService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(JobSeekerProfileDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobSeekerProfileDto>>
        GetOwnProfile(
            CancellationToken cancellationToken)
    {
        var profile =
            await _jobSeekerProfileService
                .GetOwnProfileAsync(
                    cancellationToken);

        if (profile is null)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status404NotFound,
                title:
                    "Job Seeker profile not found",
                detail:
                    "A Job Seeker profile could not be found for the authenticated user.");
        }

        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(
        typeof(JobSeekerProfileDto),
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
    public async Task<ActionResult<JobSeekerProfileDto>>
        UpdateOwnProfile(
            [FromBody]
            UpdateJobSeekerProfileRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _jobSeekerProfileService
                .UpdateOwnProfileAsync(
                    request,
                    cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Profile);
        }

        return result.FailureReason switch
        {
            JobSeekerProfileUpdateFailureReason
                    .InvalidAuthenticatedUser =>
                Problem(
                    statusCode:
                        StatusCodes.Status401Unauthorized,
                    title:
                        "Invalid authenticated user",
                    detail:
                        "The authenticated Job Seeker could not be resolved."),

            JobSeekerProfileUpdateFailureReason
                    .InvalidInput =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid Job Seeker profile",
                    detail:
                        "The submitted Job Seeker profile data is invalid."),

            JobSeekerProfileUpdateFailureReason
                    .SkillNotFound =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid skill selection",
                    detail:
                        "One or more submitted skill IDs do not reference canonical skills."),

            JobSeekerProfileUpdateFailureReason
                    .PersistenceFailed =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Job Seeker profile update failed",
                    detail:
                        "The Job Seeker profile could not be saved."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Job Seeker profile update failed")
        };
    }
}
