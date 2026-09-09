namespace HireSync.Application.Interfaces.Security;

public interface IEmailOtpCodeHasher
{
    string Hash(
        Guid challengeId,
        string code);

    bool Verify(
        Guid challengeId,
        string code,
        string expectedHash);
}
