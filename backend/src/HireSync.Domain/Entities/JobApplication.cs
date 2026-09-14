using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Entities;

public sealed class JobApplication
{
    public Guid Id { get; private set; }

    public Guid VacancyId { get; private set; }

    public Guid JobSeekerProfileId { get; private set; }

    public ApplicationStatus Status { get; private set; }

    public DateTime AppliedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private JobApplication()
    {
    }

    public JobApplication(
        Guid id,
        Guid vacancyId,
        Guid jobSeekerProfileId,
        DateTime appliedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Application ID cannot be empty.",
                nameof(id));
        }

        if (vacancyId == Guid.Empty)
        {
            throw new ArgumentException(
                "Vacancy ID cannot be empty.",
                nameof(vacancyId));
        }

        if (jobSeekerProfileId == Guid.Empty)
        {
            throw new ArgumentException(
                "Job Seeker profile ID cannot be empty.",
                nameof(jobSeekerProfileId));
        }

        EnsureUtc(
            appliedAtUtc,
            nameof(appliedAtUtc));

        Id = id;
        VacancyId = vacancyId;
        JobSeekerProfileId = jobSeekerProfileId;
        Status = ApplicationStatus.Applied;
        AppliedAtUtc = appliedAtUtc;
        UpdatedAtUtc = appliedAtUtc;
    }

    public bool ChangeStatus(
        ApplicationStatus requestedStatus,
        DateTime updatedAtUtc)
    {
        if (!Enum.IsDefined(
                typeof(ApplicationStatus),
                requestedStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedStatus),
                "Application status is invalid.");
        }

        if (ApplicationStatusRules.IsSameState(
                Status,
                requestedStatus))
        {
            return false;
        }

        if (!ApplicationStatusRules.CanTransition(
                Status,
                requestedStatus))
        {
            throw new InvalidOperationException(
                $"Application status cannot transition from {Status} to {requestedStatus}.");
        }

        EnsureUtc(
            updatedAtUtc,
            nameof(updatedAtUtc));

        Status = requestedStatus;
        UpdatedAtUtc = updatedAtUtc;

        return true;
    }

    private static void EnsureUtc(
        DateTime value,
        string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Timestamp must be UTC.",
                parameterName);
        }
    }
}
