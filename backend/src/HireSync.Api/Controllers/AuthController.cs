using HireSync.Application.DTOs.Auth;
using HireSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly RegistrationService _registrationService;

    public AuthController(
        AuthService authService,
        RegistrationService registrationService)
    {
        _authService = authService;
        _registrationService = registrationService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            request,
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Response);
        }

        return result.FailureReason switch
        {
            LoginFailureReason.Suspended => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Account suspended",
                detail: "This account is currently suspended."),

            _ => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The email or password is incorrect.")
        };
    }

    [AllowAnonymous]
    [HttpPost("register/jobseeker")]
    [ProducesResponseType(
        typeof(RegisterJobSeekerResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterJobSeekerResponse>>
        RegisterJobSeeker(
            [FromBody] RegisterJobSeekerRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _registrationService.RegisterJobSeekerAsync(
                request,
                cancellationToken);

        if (result.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                result.Response);
        }

        return result.FailureReason switch
        {
            RegistrationFailureReason.EmailAlreadyExists => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already registered",
                detail: "An account with this email already exists."),

            RegistrationFailureReason.IdentityValidationFailed => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Registration failed",
                detail: "The account details did not satisfy the identity requirements."),

            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid registration request",
                detail: "Email, password and display name are required.")
        };
    }
}
