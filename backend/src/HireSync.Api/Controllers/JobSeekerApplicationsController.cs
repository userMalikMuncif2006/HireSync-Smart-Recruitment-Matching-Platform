using HireSync.Application.DTOs.Applications;
using HireSync.Application.Interfaces.Applications;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/vacancies/{vacancyId:guid}/applications")]
[Authorize(Roles = RoleNames.JobSeeker)]
public sealed class JobSeekerApplicationsController
    : ControllerBase
{
    private readonly IJobApplicationService
        _jobApplicationService;

    private readonly ICurrentUser _currentUser;

    public JobSeekerApplicationsController(
        IJobApplicationService jobApplicationService,
        ICurrentUser currentUser)
    {
        _jobApplicationService =
            jobApplicationService;

        _currentUser =
            currentUser;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApplicationCreatedDto),
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
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApplicationCreatedDto>>
        CreateApplication(
            [FromRoute] Guid vacancyId,
            CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated ||
            !_currentUser.UserId.HasValue ||
            _currentUser.UserId.Value == Guid.Empty)
        {
            return ApplicationProblem(
                StatusCodes.Status401Unauthorized,
                "INVALID_AUTHENTICATED_USER",
                "Invalid authenticated user",
                "The authenticated Job Seeker identifier is invalid.");
        }

        var result =
            await _jobApplicationService.CreateAsync(
                _currentUser.UserId.Value,
                vacancyId,
                cancellationToken);

        if (result.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                result.Application);
        }

        return result.FailureReason switch
        {
            ApplicationCreateFailureReason.InvalidInput =>
                ApplicationProblem(
                    StatusCodes.Status400BadRequest,
                    "INVALID_APPLICATION_REQUEST",
                    "Invalid application request",
                    "The submitted application request is invalid."),

            ApplicationCreateFailureReason.JobSeekerUnavailable =>
                ApplicationProblem(
                    StatusCodes.Status403Forbidden,
                    "JOB_SEEKER_UNAVAILABLE",
                    "Job Seeker unavailable",
                    "The Job Seeker account is not eligible to apply."),

            ApplicationCreateFailureReason.ProfileNotReady =>
                ApplicationProblem(
                    StatusCodes.Status400BadRequest,
                    "PROFILE_NOT_READY",
                    "Profile not ready",
                    "Complete the structured Job Seeker profile before applying."),

            ApplicationCreateFailureReason.CurrentCvRequired =>
                ApplicationProblem(
                    StatusCodes.Status400BadRequest,
                    "CURRENT_CV_REQUIRED",
                    "Current CV required",
                    "Upload a current CV before applying."),

            ApplicationCreateFailureReason.VacancyUnavailable =>
                ApplicationProblem(
                    StatusCodes.Status404NotFound,
                    "VACANCY_UNAVAILABLE",
                    "Vacancy unavailable",
                    "The vacancy could not be found or is not available to this Job Seeker."),

            ApplicationCreateFailureReason.VacancyClosed =>
                ApplicationProblem(
                    StatusCodes.Status409Conflict,
                    "VACANCY_CLOSED",
                    "Vacancy closed",
                    "The vacancy closed before the application could be created."),

            ApplicationCreateFailureReason.AlreadyApplied =>
                ApplicationProblem(
                    StatusCodes.Status409Conflict,
                    "DUPLICATE_APPLICATION",
                    "Already applied",
                    "A Job Seeker can apply to the same vacancy only once."),

            ApplicationCreateFailureReason.PersistenceFailed =>
                ApplicationProblem(
                    StatusCodes.Status500InternalServerError,
                    "APPLICATION_PERSISTENCE_FAILED",
                    "Application creation failed",
                    "The application could not be saved."),

            _ =>
                ApplicationProblem(
                    StatusCodes.Status500InternalServerError,
                    "APPLICATION_CREATION_FAILED",
                    "Application creation failed",
                    "The application could not be created.")
        };
    }

    private static ObjectResult ApplicationProblem(
        int statusCode,
        string code,
        string title,
        string detail)
    {
        var problem =
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            };

        problem.Extensions["code"] =
            code;

        return new ObjectResult(
            problem)
        {
            StatusCode =
                statusCode
        };
    }
}