using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Employer;
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