using System.Security.Claims;
using HireSync.Infrastructure.Security;
using Microsoft.AspNetCore.Http;

namespace HireSync.Api.IntegrationTests;

public sealed class CurrentUserTests
{
    [Fact]
    public void NoHttpContext_ReturnsUnauthenticatedAndNullClaims()
    {
        var accessor = new HttpContextAccessor();
        var currentUser = new CurrentUser(accessor);

        Assert.False(currentUser.IsAuthenticated);
        Assert.Null(currentUser.UserId);
        Assert.Null(currentUser.Role);
        Assert.Null(currentUser.Email);
    }

    [Fact]
    public void AnonymousPrincipal_ReturnsUnauthenticatedAndNullClaims()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var currentUser = new CurrentUser(accessor);

        Assert.False(currentUser.IsAuthenticated);
        Assert.Null(currentUser.UserId);
        Assert.Null(currentUser.Role);
        Assert.Null(currentUser.Email);
    }

    [Fact]
    public void AuthenticatedPrincipal_ReturnsCanonicalHireSyncClaims()
    {
        var userId = Guid.NewGuid();

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("role", "JobSeeker"),
                new Claim("email", "seeker@hiresync.test")
            },
            authenticationType: "Test");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var currentUser = new CurrentUser(accessor);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal("JobSeeker", currentUser.Role);
        Assert.Equal("seeker@hiresync.test", currentUser.Email);
    }

    [Fact]
    public void AuthenticatedPrincipal_WithInvalidSubject_ReturnsNullUserId()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "not-a-guid"),
                new Claim("role", "Employer"),
                new Claim("email", "employer@hiresync.test")
            },
            authenticationType: "Test");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var currentUser = new CurrentUser(accessor);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Null(currentUser.UserId);
        Assert.Equal("Employer", currentUser.Role);
        Assert.Equal("employer@hiresync.test", currentUser.Email);
    }

    [Fact]
    public void AuthenticatedPrincipal_WithMissingOptionalClaims_ReturnsNullValues()
    {
        var userId = Guid.NewGuid();

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", userId.ToString())
            },
            authenticationType: "Test");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var currentUser = new CurrentUser(accessor);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Null(currentUser.Role);
        Assert.Null(currentUser.Email);
    }
}