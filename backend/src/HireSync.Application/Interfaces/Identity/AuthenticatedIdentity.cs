using HireSync.Domain.Enums;

namespace HireSync.Application.Interfaces.Identity;

public sealed record AuthenticatedIdentity(
    Guid UserId,
    string Email,
    string Role,
    int TokenVersion,
    AccountStatus AccountStatus);
