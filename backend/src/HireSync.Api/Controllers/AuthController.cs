using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
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
    private readonly EmployerRegistrationService _employerRegistrationService;
    private readonly IEmailOtpService _emailOtpService;
    private readonly IEmployerEmailVerificationService _employerEmailVerificationService;
    private readonly AdministratorActivationService _administratorActivationService;

    public AuthController(
        AuthService authService,
        RegistrationService registrationService,
        EmployerRegistrationService employerRegistrationService,
        IEmailOtpService emailOtpService,
        IEmployerEmailVerificationService employerEmailVerificationService,
        AdministratorActivationService administratorActivationService)
    {
        _authService = authService;
        _registrationService = registrationService;
        _employerRegistrationService = employerRegistrationService;
        _emailOtpService = emailOtpService;
        _employerEmailVerificationService = employerEmailVerificationService;
        _administratorActivationService = administratorActivationService;
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

            LoginFailureReason.AdministratorActivationRequired => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Administrator activation required",
                detail: "This Administrator account must complete first activation before signing in."),

            LoginFailureReason.EmployerEmailVerificationRequired => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Employer email verification required",
                detail: "This Employer account must verify its email before signing in."),

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
    [ProducesResponseType(
        typeof(EmployerEmailVerificationResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployerEmailVerificationResult>>
        VerifyEmployerOtp(
            [FromBody] EmployerOtpVerifyRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _employerEmailVerificationService.VerifyAsync(
                request.Email,
                request.Code,
                cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result);
        }

        return result.FailureReason switch
        {
            EmployerEmailVerificationFailureReason.Suspended =>
                Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Employer account suspended",
                    detail: "This Employer account is currently suspended."),

            EmployerEmailVerificationFailureReason.AlreadyVerified =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Employer email already verified",
                    detail: "This Employer email has already been verified."),

            EmployerEmailVerificationFailureReason.AlreadyUsed =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Verification code already used",
                    detail: "This verification code has already been consumed."),

            EmployerEmailVerificationFailureReason.AttemptsExceeded =>
                Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Verification attempt limit reached",
                    detail: "The maximum number of verification attempts has been reached."),

            EmployerEmailVerificationFailureReason.Expired =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Verification code expired",
                    detail: "The verification code has expired."),

            EmployerEmailVerificationFailureReason.PersistenceFailed =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Employer email verification failed",
                    detail: "The Employer email verification could not be saved."),

            EmployerEmailVerificationFailureReason.InvalidAccount =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Employer account",
                    detail: "A valid registered Employer account is required."),

            EmployerEmailVerificationFailureReason.InvalidRequest =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid OTP verification request",
                    detail: "Email and verification code are required."),

            _ =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid verification code",
                    detail: "The verification code is invalid.")
        };
    }
    [AllowAnonymous]
    [HttpPost("admin/activation/otp/request")]
    [ProducesResponseType(typeof(AdministratorActivationRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AdministratorActivationRequestResult>>
        RequestAdministratorActivationOtp(
            [FromBody] AdministratorActivationRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _administratorActivationService.RequestOtpAsync(
                request,
                cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result);
        }

        if (result.FailureReason ==
            AdministratorActivationRequestFailureReason.CooldownActive)
        {
            if (result.RetryAfterSeconds.HasValue)
            {
                Response.Headers["Retry-After"] =
                    result.RetryAfterSeconds.Value.ToString();
            }

            return Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Administrator activation OTP cooldown active",
                detail: "Please wait before requesting another activation code.");
        }

        return result.FailureReason switch
        {
            AdministratorActivationRequestFailureReason.InvalidCredentials =>
                Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid Administrator credentials",
                    detail: "The Administrator credentials are invalid."),

            AdministratorActivationRequestFailureReason.Suspended =>
                Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Account suspended",
                    detail: "This Administrator account is suspended."),

            AdministratorActivationRequestFailureReason.AlreadyActivated =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Administrator already activated",
                    detail: "This Administrator account has already completed first activation."),

            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Administrator activation OTP request failed",
                detail: "The activation code could not be requested.")
        };
    }

    [AllowAnonymous]
    [HttpPost("admin/activation/otp/verify")]
    [ProducesResponseType(typeof(AdministratorActivationVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AdministratorActivationVerificationResult>>
        VerifyAdministratorActivationOtp(
            [FromBody] AdministratorActivationVerifyRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _administratorActivationService.VerifyOtpAsync(
                request,
                cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result);
        }

        return result.FailureReason switch
        {
            AdministratorActivationVerificationFailureReason.InvalidCredentials =>
                Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid Administrator credentials"),

            AdministratorActivationVerificationFailureReason.Suspended =>
                Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Account suspended"),

            AdministratorActivationVerificationFailureReason.AlreadyActivated =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Administrator already activated"),

            AdministratorActivationVerificationFailureReason.AlreadyUsed =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Activation code already used"),

            AdministratorActivationVerificationFailureReason.AttemptsExceeded =>
                Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Activation attempt limit reached"),

            AdministratorActivationVerificationFailureReason.Expired =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Activation code expired"),

            AdministratorActivationVerificationFailureReason.PersistenceFailed =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Administrator activation failed"),

            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid activation code")
        };
    }
    [AllowAnonymous]
    [HttpPost("register/employer")]
    [ProducesResponseType(
        typeof(RegisterEmployerResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterEmployerResponse>>
        RegisterEmployer(
            [FromBody] RegisterEmployerRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _employerRegistrationService.RegisterAsync(
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
            EmployerRegistrationFailureReason.EmailAlreadyExists =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Email already registered",
                    detail: "An account with this email already exists."),

            EmployerRegistrationFailureReason
                .DuplicateBusinessRegistrationNumber =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Business registration number already registered",
                    detail: "An Employer profile with this business registration number already exists."),

            EmployerRegistrationFailureReason
                .IdentityValidationFailed =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Registration failed",
                    detail: "The account details did not satisfy the identity requirements."),

            EmployerRegistrationFailureReason.PersistenceFailed =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Employer registration failed",
                    detail: "The Employer account could not be created."),

            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Employer registration request",
                detail: "The Employer registration details are invalid.")
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
