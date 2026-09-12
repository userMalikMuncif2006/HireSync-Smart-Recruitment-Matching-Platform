using HireSync.Application.DTOs.Matching;
using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Matching;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Vacancy;
using HireSync.Application.Rules;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/vacancies")]
[Authorize(Roles = RoleNames.JobSeeker)]
public sealed class PublicVacanciesController : ControllerBase
{
    private readonly IVacancySearchService _vacancySearchService;
    private readonly IVacancyMatchSearchService _vacancyMatchSearchService;
    private readonly IVacancyDetailService _vacancyDetailService;
    private readonly ICurrentUser _currentUser;

    public PublicVacanciesController(
        IVacancySearchService vacancySearchService,
        IVacancyMatchSearchService vacancyMatchSearchService,
        IVacancyDetailService vacancyDetailService,
        ICurrentUser currentUser)
    {
        _vacancySearchService = vacancySearchService;
        _vacancyMatchSearchService = vacancyMatchSearchService;
        _vacancyDetailService = vacancyDetailService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PublicVacancyPageDto),
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
    public async Task<ActionResult<PublicVacancyPageDto>>
        SearchVacancies(
            [FromQuery] SearchVacanciesRequest request,
            CancellationToken cancellationToken)
    {
        if (!VacancySearchRules.IsValid(request))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid vacancy search",
                detail:
                    "The submitted vacancy search parameters are invalid.");
        }

        if (request.Sort == VacancySearchSort.Match)
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

            var matchResult =
                await _vacancyMatchSearchService
                    .SearchOpenVacanciesByMatchAsync(
                        _currentUser.UserId.Value,
                        request,
                        cancellationToken);

            if (matchResult.Succeeded)
            {
                return Ok(matchResult.Page);
            }

            return matchResult.FailureReason switch
            {
                VacancySearchFailureReason.InvalidInput =>
                    Problem(
                        statusCode:
                            StatusCodes.Status400BadRequest,
                        title:
                            "Invalid vacancy search",
                        detail:
                            "The submitted vacancy search parameters are invalid."),

                VacancySearchFailureReason.JobSeekerUnavailable =>
                    Problem(
                        statusCode:
                            StatusCodes.Status403Forbidden,
                        title:
                            "Job Seeker unavailable",
                        detail:
                            "The authenticated Job Seeker cannot use match ordering."),

                VacancySearchFailureReason.ProfileNotReady =>
                    Problem(
                        statusCode:
                            StatusCodes.Status400BadRequest,
                        title:
                            "Profile not ready",
                        detail:
                            "Complete the structured Job Seeker profile and add at least one skill before using match ordering."),

                VacancySearchFailureReason.InvalidMatchingData =>
                    Problem(
                        statusCode:
                            StatusCodes.Status500InternalServerError,
                        title:
                            "Vacancy matching failed",
                        detail:
                            "Current persisted matching data is invalid and match ordering could not be produced."),

                _ =>
                    Problem(
                        statusCode:
                            StatusCodes.Status500InternalServerError,
                        title:
                            "Vacancy matching failed",
                        detail:
                            "Match ordering could not be produced.")
            };
        }

        var result =
            await _vacancySearchService
                .SearchOpenVacanciesAsync(
                    request,
                    cancellationToken);

        return Ok(result);
    }

    [HttpGet("{vacancyId:guid}")]
    [ProducesResponseType(
        typeof(PublicVacancyDetailDto),
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
    public async Task<ActionResult<PublicVacancyDetailDto>>
        GetVacancyDetail(
            [FromRoute] Guid vacancyId,
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
            await _vacancyDetailService.GetDetailAsync(
                _currentUser.UserId.Value,
                vacancyId,
                cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Detail);
        }

        return result.FailureReason switch
        {
            VacancyDetailFailureReason.InvalidInput =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid vacancy request",
                    detail:
                        "The submitted vacancy identifier is invalid."),

            VacancyDetailFailureReason.VacancyUnavailable =>
                Problem(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title:
                        "Vacancy unavailable",
                    detail:
                        "The vacancy is closed, unavailable, or not eligible for Job Seeker viewing."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Vacancy detail failed",
                    detail:
                        "The vacancy detail could not be loaded.")
        };
    }
}