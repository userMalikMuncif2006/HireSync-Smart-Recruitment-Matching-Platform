namespace HireSync.Application.Security;

public static class RoleNames
{
    public const string JobSeeker = "JobSeeker";
    public const string Employer = "Employer";
    public const string Administrator = "Administrator";

    public static readonly string[] All =
    [
        JobSeeker,
        Employer,
        Administrator
    ];
}
