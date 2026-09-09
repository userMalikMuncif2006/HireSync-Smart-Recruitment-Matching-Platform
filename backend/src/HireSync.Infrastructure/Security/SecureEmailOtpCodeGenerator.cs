using System.Globalization;
using System.Security.Cryptography;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;

namespace HireSync.Infrastructure.Security;

public sealed class SecureEmailOtpCodeGenerator
    : IEmailOtpCodeGenerator
{
    public string Generate()
    {
        var upperBound = 1;

        for (var index = 0;
             index < EmailOtpPolicy.CodeLength;
             index++)
        {
            upperBound *= 10;
        }

        var value =
            RandomNumberGenerator.GetInt32(
                0,
                upperBound);

        return value.ToString(
            $"D{EmailOtpPolicy.CodeLength}",
            CultureInfo.InvariantCulture);
    }
}
