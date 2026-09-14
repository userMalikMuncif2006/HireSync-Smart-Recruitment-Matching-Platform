using HireSync.Application.DTOs.EmployerApplications;
using HireSync.Application.Interfaces.EmployerApplications;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/employer/vacancies/{vacancyId:guid}/applicants")]
[Authorize(Roles = RoleNames.Employer)]
public sealed class EmployerVacancyApplicantsController
    : ControllerBase
{
    private readonly IRankedApplicantService
        _rankedApplicantService;

    private readonly ICurrentUser
        _currentUser;

    public EmployerVacancyApplicantsController(
        IRankedApplicantService rankedApplicantService,
        ICurrentUser currentUser)
    {
        _rankedApplicantService =
            rankedApplicantService;

        _currentUser =
            currentUser;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(RankedApplicantPageDto),
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
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RankedApplicantPageDto>>
        GetApplicants(
            [FromRoute] Guid vacancyId,
            [FromQuery] RankedApplicantListRequest request,
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
            await _rankedApplicantService
                .GetOwnVacancyApplicantsAsync(
                    _currentUser.UserId.Value,
                    vacancyId,
                    request,
                    cancellationToken);

        if (result.Succeeded)
        {
            return Ok(
                result.Page);
        }

        return result.FailureReason switch
        {
            RankedApplicantQueryFailureReason.InvalidInput =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid applicant list request",
                    detail:
                        "The submitted applicant list parameters are invalid."),

            RankedApplicantQueryFailureReason.VacancyNotFound =>
                Problem(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title:
                        "Vacancy not found",
                    detail:
                        "The vacancy could not be found."),

            RankedApplicantQueryFailureReason.InvalidMatchingData =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Applicant ranking failed",
                    detail:
                        "Current persisted matching data is invalid and the applicant ranking could not be produced."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Applicant ranking failed",
                    detail:
                        "The applicant ranking could not be loaded.")
        };
    }
}