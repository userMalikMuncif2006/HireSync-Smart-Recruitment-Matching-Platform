using HireSync.Infrastructure.Identity;
using HireSync.Application.Interfaces.Admin;
using HireSync.Infrastructure.Admin;
using HireSync.Application.Interfaces.Email;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Otp;
using HireSync.Application.Interfaces.Persistence;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Services;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Email;
using HireSync.Infrastructure.Otp;
using HireSync.Infrastructure.Services;
using HireSync.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Shared persistence
var connectionString = builder.Configuration.GetConnectionString("HireSyncDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'HireSyncDatabase' was not found.");

builder.Services.AddDbContext<HireSyncDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IHireSyncDbContext>(serviceProvider =>
    serviceProvider.GetRequiredService<HireSyncDbContext>());

// ASP.NET Core Identity
builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<HireSyncDbContext>();

// JWT settings
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("JWT signing key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

byte[] jwtSigningKeyBytes;

try
{
    jwtSigningKeyBytes = Convert.FromBase64String(jwtSigningKey);
}
catch (FormatException exception)
{
    throw new InvalidOperationException(
        "JWT signing key must be valid Base64.",
        exception);
}

if (jwtSigningKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 256 bits.");
}

var jwtSettings = new JwtSettings
{
    SigningKey = jwtSigningKey,
    Issuer = jwtIssuer,
    Audience = jwtAudience
};

var otpHashingKey = builder.Configuration["Otp:HashingKey"]
    ?? throw new InvalidOperationException(
        "OTP hashing key is not configured.");

var otpSecuritySettings = new OtpSecuritySettings
{
    HashingKey = otpHashingKey
};

var smtpHost = builder.Configuration["Smtp:Host"]
    ?? throw new InvalidOperationException("SMTP host is not configured.");

var smtpPortValue = builder.Configuration["Smtp:Port"]
    ?? throw new InvalidOperationException("SMTP port is not configured.");

if (!int.TryParse(smtpPortValue, out var smtpPort))
{
    throw new InvalidOperationException("SMTP port is invalid.");
}

var smtpUsername = builder.Configuration["Smtp:Username"]
    ?? throw new InvalidOperationException("SMTP username is not configured.");

var smtpPassword = builder.Configuration["Smtp:Password"]
    ?? throw new InvalidOperationException("SMTP password is not configured.");

var smtpFromEmail = builder.Configuration["Smtp:FromEmail"]
    ?? throw new InvalidOperationException("SMTP from email is not configured.");

var smtpFromName =
    builder.Configuration["Smtp:FromName"] ?? "HireSync";

var smtpEmailSettings = new SmtpEmailSettings
{
    Host = smtpHost,
    Port = smtpPort,
    Username = smtpUsername,
    Password = smtpPassword,
    FromEmail = smtpFromEmail,
    FromName = smtpFromName
};
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(otpSecuritySettings);
builder.Services.AddSingleton(smtpEmailSettings);
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<IEmailOtpCodeHasher, HmacEmailOtpCodeHasher>();
builder.Services.AddSingleton<IEmailOtpCodeGenerator, SecureEmailOtpCodeGenerator>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IEmailOtpChallengeStore, EmailOtpChallengeStore>();
builder.Services.AddScoped<IAccessTokenStateValidator, AccessTokenStateValidator>();
builder.Services.AddScoped<IIdentityService, IdentityService>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<IEmployerVerificationAdminService, EmployerVerificationAdminService>();
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();

// JWT authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Preserve HireSync claim names exactly:
        // sub, email, role, token_version, jti
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSigningKeyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            NameClaimType = "email",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;

                var userIdValue =
                    principal?.FindFirst("sub")?.Value;

                var role =
                    principal?.FindFirst("role")?.Value;

                var tokenVersionValue =
                    principal?.FindFirst("token_version")?.Value;

                if (!Guid.TryParse(userIdValue, out var userId) ||
                    string.IsNullOrWhiteSpace(role) ||
                    !int.TryParse(tokenVersionValue, out var tokenVersion))
                {
                    context.Fail("Token state claims are invalid.");
                    return;
                }

                var validator =
                    context.HttpContext.RequestServices
                        .GetRequiredService<IAccessTokenStateValidator>();

                var isValid = await validator.IsValidAsync(
                    userId,
                    role,
                    tokenVersion,
                    context.HttpContext.RequestAborted);

                if (!isValid)
                {
                    context.Fail("Token is no longer valid.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var administratorSeedSettings =
    new AdministratorSeedSettings
    {
        Enabled =
            builder.Configuration.GetValue<bool>(
                "AdminSeed:Enabled"),

        Email =
            builder.Configuration["AdminSeed:Email"]
            ?? string.Empty,

        Password =
            builder.Configuration["AdminSeed:Password"]
            ?? string.Empty,

        DisplayName =
            builder.Configuration["AdminSeed:DisplayName"]
            ?? "HireSync Administrator"
    };

builder.Services.AddSingleton(administratorSeedSettings);
builder.Services.AddScoped<AdministratorSeeder>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var administratorSeeder =
        scope.ServiceProvider
            .GetRequiredService<AdministratorSeeder>();

    await administratorSeeder.SeedAsync();
}

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
