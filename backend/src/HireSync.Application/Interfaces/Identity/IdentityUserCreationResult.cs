namespace HireSync.Application.Interfaces.Identity;

public enum IdentityUserCreationFailure
{
    EmailAlreadyExists = 1,
    ValidationFailed = 2
}

public sealed record IdentityUserCreationResult
{
    private IdentityUserCreationResult(
        bool succeeded,
        Guid? userId,
        string? email,
        string? displayName,
        IdentityUserCreationFailure? failureReason)
    {
        Succeeded = succeeded;
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        FailureReason = failureReason;
    }

    public bool Succeeded { get; }

    public Guid? UserId { get; }

    public string? Email { get; }

    public string? DisplayName { get; }

    public IdentityUserCreationFailure? FailureReason { get; }

    public static IdentityUserCreationResult Success(
        Guid userId,
        string email,
        string displayName) =>
        new(true, userId, email, displayName, null);

    public static IdentityUserCreationResult Failure(
        IdentityUserCreationFailure reason) =>
        new(false, null, null, null, reason);
}
