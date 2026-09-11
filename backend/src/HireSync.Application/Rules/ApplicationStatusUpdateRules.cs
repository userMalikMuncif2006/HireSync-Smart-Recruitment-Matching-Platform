using HireSync.Application.DTOs.Applications;
using HireSync.Domain.Enums;

namespace HireSync.Application.Rules;

public static class ApplicationStatusUpdateRules
{
    public static bool HasValidRowVersion(
        byte[]? rowVersion) =>
        rowVersion is { Length: > 0 };

    public static bool IsRequestedStatusValid(
        ApplicationStatus status) =>
        Enum.IsDefined(
            typeof(ApplicationStatus),
            status);

    public static bool IsValid(
        UpdateApplicationStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return IsRequestedStatusValid(
                   request.Status) &&
               HasValidRowVersion(
                   request.RowVersion);
    }
}
