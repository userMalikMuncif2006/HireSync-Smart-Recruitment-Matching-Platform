using HireSync.Application.DTOs.ContactRequests;
using HireSync.Domain.Enums;

namespace HireSync.Application.Rules;

public static class ContactRequestWriteRules
{
    public static bool HasValidRowVersion(
        byte[]? rowVersion) =>
        rowVersion is { Length: > 0 };

    public static bool IsValidResponseStatus(
        ContactRequestStatus status) =>
        status is
            ContactRequestStatus.Accepted
            or ContactRequestStatus.Declined;

    public static bool IsValid(
        RespondContactRequestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return IsValidResponseStatus(request.Status) &&
               HasValidRowVersion(request.RowVersion);
    }
}
