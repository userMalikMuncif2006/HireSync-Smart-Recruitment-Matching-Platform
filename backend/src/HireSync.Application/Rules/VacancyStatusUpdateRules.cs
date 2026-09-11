using HireSync.Application.DTOs.Vacancy;
using HireSync.Domain.Enums;

namespace HireSync.Application.Rules;

public static class VacancyStatusUpdateRules
{
    public static bool HasValidRowVersion(byte[]? rowVersion) =>
        rowVersion is { Length: > 0 };

    public static bool IsRequestedStatusValid(VacancyStatus status) =>
        status == VacancyStatus.Closed;

    public static bool IsValid(UpdateVacancyStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return IsRequestedStatusValid(request.Status) &&
               HasValidRowVersion(request.RowVersion);
    }
}