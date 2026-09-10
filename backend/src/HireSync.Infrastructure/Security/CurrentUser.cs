using System.Security.Claims;
using HireSync.Application.Interfaces.Security;
using Microsoft.AspNetCore.Http;

namespace HireSync.Infrastructure.Security;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            var value = Principal?
                .FindFirst("sub")?
                .Value;

            return Guid.TryParse(value, out var userId)
                ? userId
                : null;
        }
    }

    public string? Role =>
        IsAuthenticated
            ? Principal?.FindFirst("role")?.Value
            : null;

    public string? Email =>
        IsAuthenticated
            ? Principal?.FindFirst("email")?.Value
            : null;
}