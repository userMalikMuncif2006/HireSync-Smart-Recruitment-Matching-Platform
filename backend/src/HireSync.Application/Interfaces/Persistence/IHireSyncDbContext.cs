namespace HireSync.Application.Interfaces.Persistence;

public interface IHireSyncDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
