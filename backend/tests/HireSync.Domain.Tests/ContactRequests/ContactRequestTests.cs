using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Domain.Tests.ContactRequests;

public sealed class ContactRequestTests
{
    [Fact]
    public void Constructor_CreatesPendingRequest()
    {
        var requestedAt = Utc(1);

        var request = Create(requestedAt);

        Assert.Equal(
            ContactRequestStatus.Pending,
            request.Status);

        Assert.Equal(
            requestedAt,
            request.RequestedAtUtc);

        Assert.Null(request.RespondedAtUtc);
    }

    [Fact]
    public void Respond_PendingToAccepted_Succeeds()
    {
        var request = Create(Utc(1));
        var respondedAt = Utc(2);

        request.Respond(
            ContactRequestStatus.Accepted,
            respondedAt);

        Assert.Equal(
            ContactRequestStatus.Accepted,
            request.Status);

        Assert.Equal(
            respondedAt,
            request.RespondedAtUtc);
    }

    [Fact]
    public void Respond_PendingToDeclined_Succeeds()
    {
        var request = Create(Utc(1));
        var respondedAt = Utc(2);

        request.Respond(
            ContactRequestStatus.Declined,
            respondedAt);

        Assert.Equal(
            ContactRequestStatus.Declined,
            request.Status);

        Assert.Equal(
            respondedAt,
            request.RespondedAtUtc);
    }

    [Theory]
    [InlineData(ContactRequestStatus.Accepted)]
    [InlineData(ContactRequestStatus.Declined)]
    public void IsTerminal_ReturnsTrueForFinalStates(
        ContactRequestStatus status)
    {
        Assert.True(
            ContactRequestRules.IsTerminal(status));
    }

    [Theory]
    [InlineData(ContactRequestStatus.Accepted)]
    [InlineData(ContactRequestStatus.Declined)]
    public void Respond_TerminalRequest_CannotTransitionAgain(
        ContactRequestStatus terminalStatus)
    {
        var request = Create(Utc(1));

        request.Respond(
            terminalStatus,
            Utc(2));

        Assert.Throws<InvalidOperationException>(
            () => request.Respond(
                terminalStatus == ContactRequestStatus.Accepted
                    ? ContactRequestStatus.Declined
                    : ContactRequestStatus.Accepted,
                Utc(3)));
    }

    [Fact]
    public void Respond_PendingToPending_IsRejected()
    {
        var request = Create(Utc(1));

        Assert.Throws<InvalidOperationException>(
            () => request.Respond(
                ContactRequestStatus.Pending,
                Utc(2)));

        Assert.Equal(
            ContactRequestStatus.Pending,
            request.Status);

        Assert.Null(request.RespondedAtUtc);
    }

    [Fact]
    public void Constructor_EmptyApplicationId_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new ContactRequest(
                ContactRequestId(),
                Guid.Empty,
                Utc(1)));
    }

    [Fact]
    public void Constructor_NonUtcTimestamp_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => Create(
                DateTime.SpecifyKind(
                    DateTime.UtcNow,
                    DateTimeKind.Local)));
    }

    [Fact]
    public void Respond_NonUtcTimestamp_IsRejectedWithoutMutation()
    {
        var request = Create(Utc(1));

        Assert.Throws<ArgumentException>(
            () => request.Respond(
                ContactRequestStatus.Accepted,
                DateTime.SpecifyKind(
                    DateTime.UtcNow,
                    DateTimeKind.Local)));

        Assert.Equal(
            ContactRequestStatus.Pending,
            request.Status);

        Assert.Null(request.RespondedAtUtc);
    }

    [Fact]
    public void EnumValues_AreCanonical()
    {
        Assert.Equal(
            1,
            (byte)ContactRequestStatus.Pending);

        Assert.Equal(
            2,
            (byte)ContactRequestStatus.Accepted);

        Assert.Equal(
            3,
            (byte)ContactRequestStatus.Declined);
    }

    [Fact]
    public void Entity_HasNoContactDisclosureFields()
    {
        var propertyNames = typeof(ContactRequest)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Email", propertyNames);
        Assert.DoesNotContain("Phone", propertyNames);
        Assert.DoesNotContain("Message", propertyNames);
        Assert.DoesNotContain("Note", propertyNames);
        Assert.DoesNotContain("Attachment", propertyNames);
    }

    private static ContactRequest Create(
        DateTime requestedAtUtc)
    {
        return new ContactRequest(
            ContactRequestId(),
            ApplicationId(),
            requestedAtUtc);
    }

    private static Guid ContactRequestId() =>
        Guid.Parse("70000000-0000-0000-0000-000000000001");

    private static Guid ApplicationId() =>
        Guid.Parse("80000000-0000-0000-0000-000000000001");

    private static DateTime Utc(int day) =>
        new(
            2026,
            9,
            day,
            12,
            0,
            0,
            DateTimeKind.Utc);
}
