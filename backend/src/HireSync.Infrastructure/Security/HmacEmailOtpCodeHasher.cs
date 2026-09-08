using System.Security.Cryptography;
using System.Text;
using HireSync.Application.Interfaces.Security;

namespace HireSync.Infrastructure.Security;

public sealed class HmacEmailOtpCodeHasher
    : IEmailOtpCodeHasher
{
    private readonly byte[] _key;

    public HmacEmailOtpCodeHasher(
        OtpSecuritySettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.HashingKey))
        {
            throw new InvalidOperationException(
                "OTP hashing key is not configured.");
        }

        try
        {
            _key = Convert.FromBase64String(
                settings.HashingKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "OTP hashing key must be valid Base64.",
                exception);
        }

        if (_key.Length < 32)
        {
            throw new InvalidOperationException(
                "OTP hashing key must contain at least 256 bits.");
        }
    }

    public string Hash(
        Guid challengeId,
        string code)
    {
        ValidateInput(challengeId, code);

        var hash = ComputeHash(
            challengeId,
            code);

        return Convert.ToBase64String(hash);
    }

    public bool Verify(
        Guid challengeId,
        string code,
        string expectedHash)
    {
        ValidateInput(challengeId, code);

        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        byte[] expectedHashBytes;

        try
        {
            expectedHashBytes =
                Convert.FromBase64String(expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHashBytes =
            ComputeHash(challengeId, code);

        return CryptographicOperations.FixedTimeEquals(
            actualHashBytes,
            expectedHashBytes);
    }

    private byte[] ComputeHash(
        Guid challengeId,
        string code)
    {
        var payload =
            $"{challengeId:N}:{code}";

        var payloadBytes =
            Encoding.UTF8.GetBytes(payload);

        using var hmac =
            new HMACSHA256(_key);

        return hmac.ComputeHash(payloadBytes);
    }

    private static void ValidateInput(
        Guid challengeId,
        string code)
    {
        if (challengeId == Guid.Empty)
        {
            throw new ArgumentException(
                "OTP challenge id is required.",
                nameof(challengeId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "OTP code is required.",
                nameof(code));
        }
    }
}
