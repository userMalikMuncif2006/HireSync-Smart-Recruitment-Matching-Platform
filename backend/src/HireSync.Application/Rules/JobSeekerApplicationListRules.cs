using HireSync.Application.DTOs.Applications;
using HireSync.Domain.Enums;

namespace HireSync.Application.Rules;

public static class JobSeekerApplicationListRules
{
    public const int PageMin = 1;

    public const int PageSizeMin = 1;
    public const int PageSizeMax = 50;

    public static bool IsValid(
        JobSeekerApplicationListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validStatus =
            !request.Status.HasValue ||
            Enum.IsDefined(
                typeof(ApplicationStatus),
                request.Status.Value);

        return validStatus &&
               request.Page >= PageMin &&
               request.PageSize >= PageSizeMin &&
               request.PageSize <= PageSizeMax;
    }
}
