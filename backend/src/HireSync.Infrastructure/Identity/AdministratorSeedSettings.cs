namespace HireSync.Infrastructure.Identity;

public sealed class AdministratorSeedSettings
{
    public bool Enabled { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string DisplayName { get; init; } = "HireSync Administrator";
}
