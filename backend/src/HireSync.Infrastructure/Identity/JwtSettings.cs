namespace HireSync.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const int AccessTokenLifetimeMinutes = 30;

    public required string SigningKey { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }
}
