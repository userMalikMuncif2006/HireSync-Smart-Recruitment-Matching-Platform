using HireSync.Application.Security;
using HireSync.Infrastructure.Security;

namespace HireSync.Api.IntegrationTests;

public class SecureEmailOtpCodeGeneratorTests
{
    [Fact]
    public void Generated_code_has_approved_length()
    {
        var generator =
            new SecureEmailOtpCodeGenerator();

        var code = generator.Generate();

        Assert.Equal(
            EmailOtpPolicy.CodeLength,
            code.Length);
    }

    [Fact]
    public void Generated_code_contains_only_ascii_digits()
    {
        var generator =
            new SecureEmailOtpCodeGenerator();

        for (var sample = 0;
             sample < 100;
             sample++)
        {
            var code = generator.Generate();

            Assert.All(
                code,
                character => Assert.InRange(
                    character,
                    '0',
                    '9'));
        }
    }
}
