namespace HireSync.Application.Interfaces.Identity;

public interface IAdministratorActivationCompleter
{
    Task<bool> MarkActivatedAsync(
        Guid administratorUserId,
        CancellationToken cancellationToken = default);
}
