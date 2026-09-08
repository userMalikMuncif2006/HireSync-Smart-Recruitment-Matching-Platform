using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;

namespace HireSync.Infrastructure.Services;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IClock _clock;

    public JwtTokenService(
        JwtSettings settings,
        IClock clock)
    {
        _settings = settings;
        _clock = clock;
    }

    public AccessTokenResult CreateAccessToken(
        Guid userId,
        string email,
        string role,
        int tokenVersion)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (!RoleNames.All.Contains(role, StringComparer.Ordinal))
        {
            throw new ArgumentException("Role is not a valid HireSync role.", nameof(role));
        }

        if (tokenVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tokenVersion),
                "Token version must be at least 1.");
        }

        byte[] signingKeyBytes;

        try
        {
            signingKeyBytes = Convert.FromBase64String(_settings.SigningKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "JWT signing key must be valid Base64.",
                exception);
        }

        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 256 bits.");
        }

        var now = _clock.UtcNow;
        var expiresAtUtc =
            now.AddMinutes(JwtSettings.AccessTokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("role", role),
            new Claim("token_version", tokenVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(signingKeyBytes),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new AccessTokenResult(token, expiresAtUtc);
    }
}
