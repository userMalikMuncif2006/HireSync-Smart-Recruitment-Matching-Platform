using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Persistence;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Services;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using HireSync.Infrastructure.Services;
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

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RegistrationService>();

// JWT authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSigningKeyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
