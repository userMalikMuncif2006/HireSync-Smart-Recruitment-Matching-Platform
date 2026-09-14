using HireSync.Infrastructure.Security;

namespace HireSync.Api.IntegrationTests;

public class HmacEmailOtpCodeHasherTests
{
    private static HmacEmailOtpCodeHasher CreateHasher()
    {
        var keyBytes =
            Enumerable.Range(1, 32)
                .Select(value => (byte)value)
                .ToArray();

        return new HmacEmailOtpCodeHasher(
            new OtpSecuritySettings
            {
                HashingKey =
                    Convert.ToBase64String(keyBytes)
            });
    }

    [Fact]
    public void Same_challenge_and_code_produce_same_hash()
    {
        var hasher = CreateHasher();

        var challengeId = Guid.NewGuid();

        var first =
            hasher.Hash(challengeId, "123456");

        var second =
            hasher.Hash(challengeId, "123456");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Different_challenge_ids_produce_different_hashes()
    {
        var hasher = CreateHasher();

        var first =
            hasher.Hash(Guid.NewGuid(), "123456");

        var second =
            hasher.Hash(Guid.NewGuid(), "123456");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Correct_code_verifies()
    {
        var hasher = CreateHasher();

        var challengeId = Guid.NewGuid();

        var hash =
            hasher.Hash(challengeId, "123456");

        Assert.True(
            hasher.Verify(
                challengeId,
                "123456",
                hash));
    }

    [Fact]
    public void Incorrect_code_does_not_verify()
    {
        var hasher = CreateHasher();

        var challengeId = Guid.NewGuid();

        var hash =
            hasher.Hash(challengeId, "123456");

        Assert.False(
            hasher.Verify(
                challengeId,
                "654321",
                hash));
    }

    [Fact]
    public void Malformed_stored_hash_does_not_verify()
    {
        var hasher = CreateHasher();

        Assert.False(
            hasher.Verify(
                Guid.NewGuid(),
                "123456",
                "not-base64"));
    }
}
