using HireSync.Application.Security;

namespace HireSync.Application.Interfaces.Security;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(
        Guid userId,
        string email,
        string role,
        int tokenVersion);
}
