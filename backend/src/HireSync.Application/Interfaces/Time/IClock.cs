namespace HireSync.Application.Interfaces.Time;

public interface IClock
{
    DateTime UtcNow { get; }
}
