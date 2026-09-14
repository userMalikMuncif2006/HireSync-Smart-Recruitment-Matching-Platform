using HireSync.Application.DTOs.Vacancy;

namespace HireSync.Application.Rules;

public static class VacancySearchRules
{
    public const int QueryMaxLength = 100;
    public const int LocationMaxLength = 100;

    public const int PageMin = 1;

    public const int PageSizeMin = 1;
    public const int PageSizeMax = 50;

    public static bool IsValidQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return query.Trim().Length <= QueryMaxLength;
    }

    public static bool IsValidLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return true;
        }

        return location.Trim().Length <= LocationMaxLength;
    }

    public static bool IsValidPage(int page) =>
        page >= PageMin;

    public static bool IsValidPageSize(int pageSize) =>
        pageSize >= PageSizeMin &&
        pageSize <= PageSizeMax;

    public static bool IsValidSort(VacancySearchSort sort) =>
        Enum.IsDefined(sort);

    public static bool IsBasicSearchSortSupported(
        VacancySearchSort sort) =>
        sort == VacancySearchSort.Newest;

    public static bool IsValid(SearchVacanciesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return IsValidQuery(request.Q) &&
               IsValidLocation(request.Location) &&
               IsValidPage(request.Page) &&
               IsValidPageSize(request.PageSize) &&
               IsValidSort(request.Sort);
    }
}