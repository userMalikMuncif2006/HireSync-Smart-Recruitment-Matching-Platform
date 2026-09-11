using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Employer;
using HireSync.Application.Rules;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/employer/vacancies")]
[Authorize(Roles = RoleNames.Employer)]
public sealed class VacanciesController : ControllerBase
{
    private readonly IVacancyService _vacancyService;

    public VacanciesController(
        IVacancyService vacancyService)
    {
        _vacancyService = vacancyService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(EmployerVacancyPageDto),
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
    public async Task<ActionResult<EmployerVacancyPageDto>>
        GetOwnVacancies(
            [FromQuery] EmployerVacancyListRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid authenticated user",
                detail: "The authenticated user identifier is invalid.");
        }

        if (!EmployerVacancyListRules.IsValid(request))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid vacancy list request",
                detail: "The submitted paging or status filter is invalid.");
        }

        var result =
            await _vacancyService.GetOwnVacanciesAsync(
                employerUserId,
                request,
                cancellationToken);

        if (result is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Employer vacancy context not found",
                detail: "The Employer profile could not be found.");
        }

        return Ok(result);
    }

    [HttpGet("{vacancyId:guid}")]
    [ProducesResponseType(
        typeof(VacancyDto),
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
    public async Task<ActionResult<VacancyDto>>
        GetOwnVacancy(
            [FromRoute] Guid vacancyId,
            CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid authenticated user",
                detail: "The authenticated user identifier is invalid.");
        }

        var result =
            await _vacancyService.GetOwnVacancyAsync(
                employerUserId,
                vacancyId,
                cancellationToken);

        if (result is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Vacancy not found",
                detail: "The vacancy could not be found.");
        }

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(VacancyDto),
        StatusCodes.Status201Created)]
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
    public async Task<ActionResult<VacancyDto>>
        CreateOwnVacancy(
            [FromBody] CreateVacancyRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid authenticated user",
                detail: "The authenticated user identifier is invalid.");
        }

        var result =
            await _vacancyService.CreateOwnVacancyAsync(
                employerUserId,
                request,
                cancellationToken);

        if (result.Succeeded)
        {
            return Created(
                $"/api/v1/employer/vacancies/{result.Vacancy!.Id}",
                result.Vacancy);
        }

        return result.FailureReason switch
        {
            VacancyCreateFailureReason.InvalidInput =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid vacancy",
                    detail: "The submitted vacancy data is invalid."),

            VacancyCreateFailureReason.InvalidRequiredSkills =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid required skills",
                    detail: "One or more required skills are invalid."),

            VacancyCreateFailureReason.EmployerNotReady =>
                Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Employer is not ready to create vacancies",
                    detail: "The Employer account and company profile must be eligible before creating a vacancy."),

            VacancyCreateFailureReason.PersistenceFailed =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Vacancy creation failed",
                    detail: "The vacancy could not be saved."),

            _ =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Vacancy creation failed")
        };
    }

    [HttpPut("{vacancyId:guid}")]
    [ProducesResponseType(
        typeof(VacancyDto),
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
    public async Task<ActionResult<VacancyDto>>
        UpdateOwnVacancy(
            [FromRoute] Guid vacancyId,
            [FromBody] UpdateVacancyRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid authenticated user",
                detail: "The authenticated user identifier is invalid.");
        }

        var result =
            await _vacancyService.UpdateOwnVacancyAsync(
                employerUserId,
                vacancyId,
                request,
                cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Vacancy);
        }

        return result.FailureReason switch
        {
            VacancyUpdateFailureReason.InvalidInput =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid vacancy update",
                    detail: "The submitted vacancy update is invalid."),

            VacancyUpdateFailureReason.InvalidRequiredSkills =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid required skills",
                    detail: "One or more required skills are invalid."),

            VacancyUpdateFailureReason.NotFound =>
                Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Vacancy not found",
                    detail: "The vacancy could not be found."),

            VacancyUpdateFailureReason.Closed =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Vacancy is closed",
                    detail: "A Closed vacancy is read-only and cannot be updated."),

            VacancyUpdateFailureReason.ConcurrencyConflict =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Vacancy was modified",
                    detail: "The vacancy has changed since it was loaded. Reload it and try again."),

            VacancyUpdateFailureReason.PersistenceFailed =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Vacancy update failed",
                    detail: "The vacancy could not be saved."),

            _ =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Vacancy update failed")
        };
    }

    [HttpPatch("{vacancyId:guid}/status")]
    [ProducesResponseType(
        typeof(VacancyStatusDto),
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
    public async Task<ActionResult<VacancyStatusDto>>
        UpdateOwnVacancyStatus(
            [FromRoute] Guid vacancyId,
            [FromBody] UpdateVacancyStatusRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid authenticated user",
                detail: "The authenticated user identifier is invalid.");
        }

        var result =
            await _vacancyService.UpdateOwnVacancyStatusAsync(
                employerUserId,
                vacancyId,
                request,
                cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Vacancy);
        }

        return result.FailureReason switch
        {
            VacancyStatusUpdateFailureReason.InvalidInput =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid vacancy status update",
                    detail: "The submitted vacancy status update is invalid."),

            VacancyStatusUpdateFailureReason.NotFound =>
                Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Vacancy not found",
                    detail: "The vacancy could not be found."),

            VacancyStatusUpdateFailureReason.AlreadyClosed =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Vacancy already closed",
                    detail: "A Closed vacancy cannot be closed again."),

            VacancyStatusUpdateFailureReason.ConcurrencyConflict =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Vacancy was modified",
                    detail: "The vacancy has changed since it was loaded. Reload it and try again."),

            VacancyStatusUpdateFailureReason.PersistenceFailed =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Vacancy status update failed",
                    detail: "The vacancy status could not be saved."),

            _ =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Vacancy status update failed")
        };
    }

    private bool TryGetEmployerUserId(
        out Guid employerUserId)
    {
        var value = User.FindFirst("sub")?.Value;

        return Guid.TryParse(
            value,
            out employerUserId);
    }
}