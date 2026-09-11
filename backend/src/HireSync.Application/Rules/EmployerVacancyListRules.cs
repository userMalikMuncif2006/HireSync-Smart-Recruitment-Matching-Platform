using HireSync.Application.DTOs.Vacancy;

namespace HireSync.Application.Rules;

public static class EmployerVacancyListRules
{
    public const int PageMin = 1;

    public const int PageSizeMin = 1;
    public const int PageSizeMax = 50;

    public static bool IsValid(
        EmployerVacancyListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validStatus =
            !request.Status.HasValue ||
            Enum.IsDefined(request.Status.Value);

        return validStatus &&
               request.Page >= PageMin &&
               request.PageSize >= PageSizeMin &&
               request.PageSize <= PageSizeMax;
    }
}