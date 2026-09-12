using HireSync.Application.DTOs.ContactRequests;
using HireSync.Application.Interfaces.ContactRequests;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/employer/applications")]
[Authorize(Roles = RoleNames.Employer)]
public sealed class EmployerContactRequestsController
    : ControllerBase
{
    private readonly IContactRequestService
        _contactRequestService;

    private readonly ICurrentUser _currentUser;

    public EmployerContactRequestsController(
        IContactRequestService contactRequestService,
        ICurrentUser currentUser)
    {
        _contactRequestService =
            contactRequestService;

        _currentUser =
            currentUser;
    }

    [HttpPost("{jobApplicationId:guid}/contact-requests")]
    [ProducesResponseType(
        typeof(ContactRequestDto),
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
    public async Task<ActionResult<ContactRequestDto>>
        CreateContactRequest(
            [FromRoute] Guid jobApplicationId,
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
            await _contactRequestService
                .CreateForOwnApplicationAsync(
                    _currentUser.UserId.Value,
                    jobApplicationId,
                    cancellationToken);

        if (result.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                result.ContactRequest);
        }

        return result.FailureReason switch
        {
            ContactRequestWriteFailureReason.InvalidInput =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid contact request",
                    detail:
                        "The submitted contact request is invalid."),

            ContactRequestWriteFailureReason.NotFound =>
                Problem(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title:
                        "Application not found",
                    detail:
                        "The application could not be found."),

            ContactRequestWriteFailureReason.ApplicationRejected =>
                Problem(
                    statusCode:
                        StatusCodes.Status409Conflict,
                    title:
                        "Application is rejected",
                    detail:
                        "A contact request cannot be created for a rejected application."),

            ContactRequestWriteFailureReason.ParticipantInactive =>
                Problem(
                    statusCode:
                        StatusCodes.Status409Conflict,
                    title:
                        "Contact request is not allowed",
                    detail:
                        "Both participants must have Active accounts."),

            ContactRequestWriteFailureReason.AlreadyExists =>
                Problem(
                    statusCode:
                        StatusCodes.Status409Conflict,
                    title:
                        "Contact request already exists",
                    detail:
                        "Only one contact request is allowed per application."),

            ContactRequestWriteFailureReason.PersistenceFailed =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Contact request creation failed",
                    detail:
                        "The contact request could not be saved."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Contact request creation failed")
        };
    }
}
