namespace HireSync.Application.Interfaces.Security;

public interface IAccessTokenStateValidator
{
    Task<bool> IsValidAsync(
        Guid userId,
        string role,
        int tokenVersion,
        CancellationToken cancellationToken = default);
}
