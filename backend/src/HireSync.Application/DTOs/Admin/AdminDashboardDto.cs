namespace HireSync.Application.DTOs.Admin;

public sealed record AdminDashboardDto(
    int TotalUsers,
    int TotalVacancies,
    int TotalApplications,
    DateTime CalculatedAtUtc);