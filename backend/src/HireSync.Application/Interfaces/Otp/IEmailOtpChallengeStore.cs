using HireSync.Domain.Entities;
using HireSync.Domain.Enums;

namespace HireSync.Application.Interfaces.Otp;

public interface IEmailOtpChallengeStore
{
    Task AddAsync(
        EmailOtpChallenge challenge,
        CancellationToken cancellationToken = default);

    Task<EmailOtpChallenge?> GetLatestAsync(
        string email,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
