using HireSync.Application.Interfaces.Otp;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Otp;

public sealed class EmailOtpChallengeStore
    : IEmailOtpChallengeStore
{
    private readonly HireSyncDbContext _dbContext;

    public EmailOtpChallengeStore(
        HireSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        EmailOtpChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        await _dbContext.EmailOtpChallenges.AddAsync(
            challenge,
            cancellationToken);
    }

    public Task<EmailOtpChallenge?> GetLatestAsync(
        string email,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Email is required.",
                nameof(email));
        }

        return _dbContext.EmailOtpChallenges
            .Where(challenge =>
                challenge.Email == email.Trim() &&
                challenge.Purpose == purpose)
            .OrderByDescending(challenge =>
                challenge.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
