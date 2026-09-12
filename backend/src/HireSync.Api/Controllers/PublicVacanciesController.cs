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
    private readonly IVacancyDetailService _vacancyDetailService;
    private readonly ICurrentUser _currentUser;

    public PublicVacanciesController(
        IVacancySearchService vacancySearchService,
        IVacancyDetailService vacancyDetailService,
        ICurrentUser currentUser)
    {
        _vacancySearchService = vacancySearchService;
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

        if (!VacancySearchRules.IsBasicSearchSortSupported(
                request.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Unsupported vacancy sort",
                detail:
                    "Match sorting is not available until the matching workflow is integrated.");
        }

        var result =
            await _vacancySearchService.SearchOpenVacanciesAsync(
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
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid authenticated user",
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
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid vacancy request",
                    detail:
                        "The submitted vacancy identifier is invalid."),

            VacancyDetailFailureReason.VacancyUnavailable =>
                Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Vacancy unavailable",
                    detail:
                        "The vacancy is closed, unavailable, or not eligible for Job Seeker viewing."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title: "Vacancy detail failed",
                    detail:
                        "The vacancy detail could not be loaded.")
        };
    }
}