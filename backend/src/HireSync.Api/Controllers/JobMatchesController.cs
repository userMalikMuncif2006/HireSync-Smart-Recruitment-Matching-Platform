using HireSync.Application.DTOs.Matching;
using HireSync.Application.Interfaces.Matching;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/vacancies")]
[Authorize(Roles = RoleNames.JobSeeker)]
public sealed class JobMatchesController : ControllerBase
{
    private readonly IJobMatchService _jobMatchService;
    private readonly ICurrentUser _currentUser;

    public JobMatchesController(
        IJobMatchService jobMatchService,
        ICurrentUser currentUser)
    {
        _jobMatchService = jobMatchService;
        _currentUser = currentUser;
    }

    [HttpGet("{vacancyId:guid}/match")]
    [ProducesResponseType(
        typeof(MatchResultDto),
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
    public async Task<ActionResult<MatchResultDto>>
        GetMatch(
            [FromRoute] Guid vacancyId,
            CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated ||
            !_currentUser.UserId.HasValue ||
            _currentUser.UserId.Value == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid authenticated user",
                detail:
                    "The authenticated Job Seeker identifier is invalid.");
        }

        var result =
            await _jobMatchService.GetMatchAsync(
                _currentUser.UserId.Value,
                vacancyId,
                cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Match);
        }

        return result.FailureReason switch
        {
            JobMatchFailureReason.InvalidInput =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid match request",
                    detail:
                        "The submitted vacancy identifier is invalid."),

            JobMatchFailureReason.JobSeekerNotReady =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Job Seeker is not match ready",
                    detail:
                        "Complete the structured Job Seeker profile and select at least one skill before viewing a match."),

            JobMatchFailureReason.VacancyUnavailable =>
                Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Vacancy unavailable",
                    detail:
                        "The vacancy is unavailable for matching."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title: "Match calculation failed",
                    detail:
                        "The match result could not be calculated.")
        };
    }
}
