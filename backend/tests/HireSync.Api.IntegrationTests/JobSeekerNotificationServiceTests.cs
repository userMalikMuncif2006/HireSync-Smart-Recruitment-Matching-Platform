using HireSync.Application.DTOs.Notifications;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using HireSync.Infrastructure.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Api.IntegrationTests;

public sealed class JobSeekerNotificationServiceTests
{
    private static readonly DateTime BaseUtc =
        new(
            2026,
            9,
            12,
            11,
            0,
            0,
            DateTimeKind.Utc);

    [Fact]
    public async Task List_returns_only_own_notifications()
    {
        await using var context =
            await CreateContextAsync();

        var ownUser =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var foreignUser =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var own =
            AddNotification(
                context,
                ownUser.Id,
                BaseUtc.AddMinutes(2));

        _ =
            AddNotification(
                context,
                foreignUser.Id,
                BaseUtc.AddMinutes(3));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service =
            CreateService(
                context);

        var result =
            await service
                .GetOwnNotificationsAsync(
                    ownUser.Id,
                    new NotificationListRequest());

        Assert.True(result.Succeeded);

        var item =
            Assert.Single(
                result.Page!.Items);

        Assert.Equal(
            own.Id,
            item.Id);

        Assert.Equal(
            NotificationType.ApplicationStatusChanged,
            item.Type);
    }

    [Fact]
    public async Task List_is_newest_first_with_deterministic_id_tie_break()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var older =
            AddNotification(
                context,
                user.Id,
                BaseUtc,
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000003"));

        var tieSecond =
            AddNotification(
                context,
                user.Id,
                BaseUtc.AddMinutes(5),
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002"));

        var tieFirst =
            AddNotification(
                context,
                user.Id,
                BaseUtc.AddMinutes(5),
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001"));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service =
            CreateService(
                context);

        var result =
            await service
                .GetOwnNotificationsAsync(
                    user.Id,
                    new NotificationListRequest());

        Assert.True(result.Succeeded);

        Assert.Equal(
            new[]
            {
                tieFirst.Id,
                tieSecond.Id,
                older.Id
            },
            result.Page!.Items.Select(
                item =>
                    item.Id));
    }

    [Fact]
    public async Task Empty_list_is_successful()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        context.ChangeTracker.Clear();

        var service =
            CreateService(
                context);

        var result =
            await service
                .GetOwnNotificationsAsync(
                    user.Id,
                    new NotificationListRequest());

        Assert.True(result.Succeeded);
        Assert.Empty(result.Page!.Items);
        Assert.Equal(
            0,
            result.Page.TotalCount);
    }

    [Fact]
    public async Task Pagination_validation_and_page_selection_are_enforced()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        _ =
            AddNotification(
                context,
                user.Id,
                BaseUtc.AddMinutes(3));

        var second =
            AddNotification(
                context,
                user.Id,
                BaseUtc.AddMinutes(2));

        _ =
            AddNotification(
                context,
                user.Id,
                BaseUtc.AddMinutes(1));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service =
            CreateService(
                context);

        var invalid =
            await service
                .GetOwnNotificationsAsync(
                    user.Id,
                    new NotificationListRequest(
                        Page: 0,
                        PageSize: 51));

        Assert.False(
            invalid.Succeeded);

        Assert.Equal(
            NotificationReadFailureReason.InvalidInput,
            invalid.FailureReason);

        var page =
            await service
                .GetOwnNotificationsAsync(
                    user.Id,
                    new NotificationListRequest(
                        Page: 2,
                        PageSize: 1));

        Assert.True(page.Succeeded);
        Assert.Equal(
            3,
            page.Page!.TotalCount);

        var item =
            Assert.Single(
                page.Page.Items);

        Assert.Equal(
            second.Id,
            item.Id);
    }

    [Fact]
    public async Task Mark_one_sets_read_state_and_utc_timestamp()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var notification =
            AddNotification(
                context,
                user.Id,
                BaseUtc);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var readAt =
            BaseUtc.AddMinutes(10);

        var clock =
            new TestClock(
                readAt);

        var service =
            new JobSeekerNotificationService(
                context,
                clock);

        var result =
            await service.MarkReadAsync(
                user.Id,
                notification.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(
            1,
            result.ChangedCount);

        var stored =
            await context.Notifications
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                            notification.Id);

        Assert.True(stored.IsRead);
        Assert.Equal(
            readAt,
            stored.ReadAtUtc);

        Assert.Equal(
            DateTimeKind.Utc,
            stored.ReadAtUtc!.Value.Kind);
    }

    [Fact]
    public async Task Foreign_notification_cannot_be_read()
    {
        await using var context =
            await CreateContextAsync();

        var ownUser =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var foreignUser =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var foreign =
            AddNotification(
                context,
                foreignUser.Id,
                BaseUtc);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service =
            CreateService(
                context);

        var result =
            await service.MarkReadAsync(
                ownUser.Id,
                foreign.Id);

        Assert.False(result.Succeeded);

        Assert.Equal(
            NotificationReadFailureReason
                .NotificationNotFound,
            result.FailureReason);

        var stored =
            await context.Notifications
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                            foreign.Id);

        Assert.False(stored.IsRead);
        Assert.Null(stored.ReadAtUtc);
    }

    [Fact]
    public async Task Mark_all_updates_only_own_unread_notifications()
    {
        await using var context =
            await CreateContextAsync();

        var ownUser =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var foreignUser =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var first =
            AddNotification(
                context,
                ownUser.Id,
                BaseUtc);

        var second =
            AddNotification(
                context,
                ownUser.Id,
                BaseUtc.AddMinutes(1));

        var foreign =
            AddNotification(
                context,
                foreignUser.Id,
                BaseUtc.AddMinutes(2));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var readAt =
            BaseUtc.AddMinutes(20);

        var service =
            new JobSeekerNotificationService(
                context,
                new TestClock(readAt));

        var result =
            await service.MarkAllReadAsync(
                ownUser.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(
            2,
            result.ChangedCount);

        var stored =
            await context.Notifications
                .ToDictionaryAsync(
                    notification =>
                        notification.Id);

        Assert.True(stored[first.Id].IsRead);
        Assert.True(stored[second.Id].IsRead);

        Assert.Equal(
            readAt,
            stored[first.Id].ReadAtUtc);

        Assert.Equal(
            readAt,
            stored[second.Id].ReadAtUtc);

        Assert.False(
            stored[foreign.Id].IsRead);

        Assert.Null(
            stored[foreign.Id].ReadAtUtc);
    }

    [Fact]
    public async Task Repeated_mark_read_is_idempotent_and_preserves_timestamp()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var notification =
            AddNotification(
                context,
                user.Id,
                BaseUtc);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var firstReadAt =
            BaseUtc.AddMinutes(5);

        var clock =
            new TestClock(
                firstReadAt);

        var service =
            new JobSeekerNotificationService(
                context,
                clock);

        var first =
            await service.MarkReadAsync(
                user.Id,
                notification.Id);

        Assert.True(first.Succeeded);
        Assert.Equal(
            1,
            first.ChangedCount);

        clock.UtcNow =
            BaseUtc.AddHours(1);

        var second =
            await service.MarkReadAsync(
                user.Id,
                notification.Id);

        Assert.True(second.Succeeded);
        Assert.Equal(
            0,
            second.ChangedCount);

        var stored =
            await context.Notifications
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                            notification.Id);

        Assert.Equal(
            firstReadAt,
            stored.ReadAtUtc);
    }

    [Fact]
    public async Task Wrong_role_and_inactive_job_seeker_are_rejected()
    {
        await using var context =
            await CreateContextAsync();

        var employer =
            await AddUserWithRoleAsync(
                context,
                RoleNames.Employer,
                AccountStatus.Active);

        var suspended =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Suspended);

        context.ChangeTracker.Clear();

        var service =
            CreateService(
                context);

        var employerResult =
            await service.GetOwnNotificationsAsync(
                employer.Id,
                new NotificationListRequest());

        Assert.False(
            employerResult.Succeeded);

        Assert.Equal(
            NotificationReadFailureReason
                .JobSeekerUnavailable,
            employerResult.FailureReason);

        var suspendedResult =
            await service.GetOwnNotificationsAsync(
                suspended.Id,
                new NotificationListRequest());

        Assert.False(
            suspendedResult.Succeeded);

        Assert.Equal(
            NotificationReadFailureReason
                .JobSeekerUnavailable,
            suspendedResult.FailureReason);
    }

    private static JobSeekerNotificationService
        CreateService(
            HireSyncDbContext context)
    {
        return new JobSeekerNotificationService(
            context,
            new TestClock(
                BaseUtc.AddHours(1)));
    }

    private static async Task<HireSyncDbContext>
        CreateContextAsync()
    {
        var options =
            new DbContextOptionsBuilder<
                    HireSyncDbContext>()
                .UseInMemoryDatabase(
                    $"HireSync-Notifications-{Guid.NewGuid():N}")
                .Options;

        var context =
            new HireSyncDbContext(
                options);

        await context.Database
            .EnsureCreatedAsync();

        return context;
    }

    private static async Task<ApplicationUser>
        AddUserWithRoleAsync(
            HireSyncDbContext context,
            string roleName,
            AccountStatus accountStatus)
    {
        var user =
            new ApplicationUser
            {
                Id =
                    Guid.NewGuid(),
                UserName =
                    $"{Guid.NewGuid():N}@hiresync.test",
                NormalizedUserName =
                    $"{Guid.NewGuid():N}@HIRESYNC.TEST",
                Email =
                    $"{Guid.NewGuid():N}@hiresync.test",
                NormalizedEmail =
                    $"{Guid.NewGuid():N}@HIRESYNC.TEST",
                DisplayName =
                    "Notification Test User",
                AccountStatus =
                    accountStatus,
                CreatedAtUtc =
                    BaseUtc,
                UpdatedAtUtc =
                    BaseUtc
            };

        var role =
            await context.Roles
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Name ==
                            roleName);

        if (role is null)
        {
            role =
                new IdentityRole<Guid>
                {
                    Id =
                        Guid.NewGuid(),
                    Name =
                        roleName,
                    NormalizedName =
                        roleName.ToUpperInvariant()
                };

            context.Roles.Add(
                role);
        }

        context.Users.Add(
            user);

        context.UserRoles.Add(
            new IdentityUserRole<Guid>
            {
                UserId =
                    user.Id,
                RoleId =
                    role.Id
            });

        await context.SaveChangesAsync();

        return user;
    }

    private static Notification AddNotification(
        HireSyncDbContext context,
        Guid recipientUserId,
        DateTime createdAtUtc,
        Guid? notificationId = null)
    {
        var notification =
            new Notification(
                notificationId ??
                    Guid.NewGuid(),
                recipientUserId,
                NotificationType
                    .ApplicationStatusChanged,
                "Application status changed",
                "Your application status has changed.",
                Guid.NewGuid(),
                createdAtUtc);

        context.Notifications.Add(
            notification);

        return notification;
    }

    private sealed class TestClock
        : IClock
    {
        public TestClock(
            DateTime utcNow)
        {
            UtcNow =
                utcNow;
        }

        public DateTime UtcNow
        {
            get;
            set;
        }
    }
}
