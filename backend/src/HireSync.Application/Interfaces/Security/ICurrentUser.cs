namespace HireSync.Application.Interfaces.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Role { get; }

    string? Email { get; }
}