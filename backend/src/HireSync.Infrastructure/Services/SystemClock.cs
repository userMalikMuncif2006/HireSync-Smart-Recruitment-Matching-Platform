using HireSync.Application.Interfaces.Time;

namespace HireSync.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
