using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Otp;
using HireSync.Application.Services;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly RegistrationService _registrationService;
    private readonly IEmailOtpService _emailOtpService;

    public AuthController(
        AuthService authService,
        RegistrationService registrationService,
        IEmailOtpService emailOtpService)
    {
        _authService = authService;
        _registrationService = registrationService;
        _emailOtpService = emailOtpService;
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
    [HttpPost("employer/otp/request")]
    [ProducesResponseType(typeof(OtpRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<OtpRequestResult>> RequestEmployerOtp(
        [FromBody] EmployerOtpRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid OTP request",
                detail: "Email is required.");
        }

        var result = await _emailOtpService.RequestAsync(
            request.Email,
            EmailOtpPurpose.EmployerRegistration,
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result);
        }

        if (result.FailureReason ==
            OtpRequestFailureReason.CooldownActive)
        {
            if (result.RetryAfterSeconds.HasValue)
            {
                Response.Headers["Retry-After"] =
                    result.RetryAfterSeconds.Value.ToString();
            }

            return Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "OTP request cooldown active",
                detail: "Please wait before requesting another verification code.");
        }

        return Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "OTP request failed",
            detail: "The verification code could not be requested.");
    }
    [AllowAnonymous]
    [HttpPost("employer/otp/verify")]
    [ProducesResponseType(typeof(OtpVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<OtpVerificationResult>> VerifyEmployerOtp(
        [FromBody] EmployerOtpVerifyRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Code))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid OTP verification request",
                detail: "Email and verification code are required.");
        }

        var result = await _emailOtpService.VerifyAsync(
            request.Email,
            EmailOtpPurpose.EmployerRegistration,
            request.Code,
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result);
        }

        return result.FailureReason switch
        {
            OtpVerificationFailureReason.AlreadyUsed => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Verification code already used",
                detail: "This verification code has already been consumed."),

            OtpVerificationFailureReason.AttemptsExceeded => Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Verification attempt limit reached",
                detail: "The maximum number of verification attempts has been reached."),

            OtpVerificationFailureReason.Expired => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Verification code expired",
                detail: "The verification code has expired."),

            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid verification code",
                detail: "The verification code is invalid.")
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
