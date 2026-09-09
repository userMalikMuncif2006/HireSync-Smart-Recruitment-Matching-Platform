using System.IdentityModel.Tokens.Jwt;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Infrastructure.Identity;
using HireSync.Infrastructure.Services;
using Microsoft.IdentityModel.Tokens;

namespace HireSync.Api.IntegrationTests;

public class JwtTokenServiceContractTests
{
    [Fact]
    public void CreateAccessToken_emits_frozen_HireSync_contract()
    {
        var now = new DateTime(
            2026, 9, 8, 10, 0, 0,
            DateTimeKind.Utc);

        var settings = new JwtSettings
        {
            SigningKey = Convert.ToBase64String(
                Enumerable.Repeat((byte)7, 32).ToArray()),
            Issuer = "HireSync.Api",
            Audience = "HireSync.Web"
        };

        var service = new JwtTokenService(
            settings,
            new FakeClock(now));

        var userId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        var result = service.CreateAccessToken(
            userId,
            "jobseeker@example.com",
            RoleNames.JobSeeker,
            3);

        var jwt =
            new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal(
            now.AddMinutes(JwtSettings.AccessTokenLifetimeMinutes),
            result.ExpiresAtUtc);

        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
        Assert.Equal("HireSync.Api", jwt.Issuer);
        Assert.Contains("HireSync.Web", jwt.Audiences);

        Assert.Equal(
            userId.ToString(),
            jwt.Claims.Single(
                claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);

        Assert.Equal(
            "jobseeker@example.com",
            jwt.Claims.Single(
                claim => claim.Type == JwtRegisteredClaimNames.Email).Value);

        Assert.Equal(
            RoleNames.JobSeeker,
            jwt.Claims.Single(
                claim => claim.Type == "role").Value);

        Assert.Equal(
            "3",
            jwt.Claims.Single(
                claim => claim.Type == "token_version").Value);

        Assert.False(
            string.IsNullOrWhiteSpace(
                jwt.Claims.Single(
                    claim => claim.Type == JwtRegisteredClaimNames.Jti).Value));
    }

    [Fact]
    public void CreateAccessToken_rejects_non_HireSync_role()
    {
        var settings = new JwtSettings
        {
            SigningKey = Convert.ToBase64String(
                Enumerable.Repeat((byte)7, 32).ToArray()),
            Issuer = "HireSync.Api",
            Audience = "HireSync.Web"
        };

        var service = new JwtTokenService(
            settings,
            new FakeClock(DateTime.UtcNow));

        Assert.Throws<ArgumentException>(() =>
            service.CreateAccessToken(
                Guid.NewGuid(),
                "user@example.com",
                "SuperAdmin",
                1));
    }

    private sealed class FakeClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
