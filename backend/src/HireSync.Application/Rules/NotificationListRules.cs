using HireSync.Application.DTOs.Notifications;

namespace HireSync.Application.Rules;

public static class NotificationListRules
{
    public const int PageMin = 1;

    public const int PageSizeMin = 1;
    public const int PageSizeMax = 50;

    public static bool IsValid(
        NotificationListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Page >= PageMin &&
               request.PageSize >= PageSizeMin &&
               request.PageSize <= PageSizeMax;
    }
}
