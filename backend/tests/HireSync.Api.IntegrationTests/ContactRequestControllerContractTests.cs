using HireSync.Api.Controllers;
using HireSync.Application.DTOs.ContactRequests;
using HireSync.Application.Interfaces.ContactRequests;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class ContactRequestControllerContractTests
{
    [Fact]
    public void Employer_controller_requires_Employer_role()
    {
        var authorize =
            typeof(EmployerContactRequestsController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute),
                    inherit: true)
                .Cast<AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            RoleNames.Employer,
            authorize.Roles);
    }

    [Fact]
    public void Employer_controller_uses_expected_create_route()
    {
        var route =
            typeof(EmployerContactRequestsController)
                .GetCustomAttributes(
                    typeof(RouteAttribute),
                    inherit: true)
                .Cast<RouteAttribute>()
                .Single();

        Assert.Equal(
            "api/v1/employer/applications",
            route.Template);

        var method =
            typeof(EmployerContactRequestsController)
                .GetMethod(
                    nameof(
                        EmployerContactRequestsController
                            .CreateContactRequest));

        Assert.NotNull(method);

        var post =
            method!
                .GetCustomAttributes(
                    typeof(HttpPostAttribute),
                    inherit: true)
                .Cast<HttpPostAttribute>()
                .Single();

        Assert.Equal(
            "{jobApplicationId:guid}/contact-request",
            post.Template);
    }

    [Fact]
    public async Task Employer_create_returns_201_and_forwards_current_user()
    {
        var employerUserId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        var expected =
            CreateContactRequestDto(
                applicationId,
                ContactRequestStatus.Pending);

        var service =
            new FakeContactRequestService
            {
                CreateResult =
                    ContactRequestWriteResult
                        .Success(expected)
            };

        var controller =
            new EmployerContactRequestsController(
                service,
                new FakeCurrentUser(
                    employerUserId,
                    RoleNames.Employer));

        var action =
            await controller.CreateContactRequest(
                applicationId,
                CancellationToken.None);

        var result =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            result.StatusCode);

        Assert.Same(
            expected,
            result.Value);

        Assert.Equal(
            employerUserId,
            service.LastEmployerUserId);

        Assert.Equal(
            applicationId,
            service.LastApplicationId);
    }

    [Fact]
    public async Task Employer_create_returns_401_for_invalid_current_user()
    {
        var controller =
            new EmployerContactRequestsController(
                new FakeContactRequestService(),
                new FakeCurrentUser(
                    null,
                    RoleNames.Employer));

        var action =
            await controller.CreateContactRequest(
                Guid.NewGuid(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);
    }

    [Theory]
    [InlineData(
        ContactRequestWriteFailureReason.InvalidInput,
        StatusCodes.Status400BadRequest)]
    [InlineData(
        ContactRequestWriteFailureReason.NotFound,
        StatusCodes.Status404NotFound)]
    [InlineData(
        ContactRequestWriteFailureReason.ApplicationRejected,
        StatusCodes.Status409Conflict)]
    [InlineData(
        ContactRequestWriteFailureReason.ParticipantInactive,
        StatusCodes.Status403Forbidden)]
    [InlineData(
        ContactRequestWriteFailureReason.AlreadyExists,
        StatusCodes.Status409Conflict)]
    [InlineData(
        ContactRequestWriteFailureReason.PersistenceFailed,
        StatusCodes.Status500InternalServerError)]
    public async Task Employer_create_maps_failure_to_expected_status(
        ContactRequestWriteFailureReason failureReason,
        int expectedStatus)
    {
        var service =
            new FakeContactRequestService
            {
                CreateResult =
                    ContactRequestWriteResult
                        .Failure(failureReason)
            };

        var controller =
            new EmployerContactRequestsController(
                service,
                new FakeCurrentUser(
                    Guid.NewGuid(),
                    RoleNames.Employer));

        var action =
            await controller.CreateContactRequest(
                Guid.NewGuid(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            expectedStatus,
            problem.StatusCode);
    }

    [Fact]
    public void JobSeeker_controller_requires_JobSeeker_role()
    {
        var authorize =
            typeof(JobSeekerContactRequestsController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute),
                    inherit: true)
                .Cast<AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            RoleNames.JobSeeker,
            authorize.Roles);
    }

    [Fact]
    public void JobSeeker_controller_uses_expected_response_route()
    {
        var route =
            typeof(JobSeekerContactRequestsController)
                .GetCustomAttributes(
                    typeof(RouteAttribute),
                    inherit: true)
                .Cast<RouteAttribute>()
                .Single();

        Assert.Equal(
            "api/v1/seeker/contact-requests",
            route.Template);

        var method =
            typeof(JobSeekerContactRequestsController)
                .GetMethod(
                    nameof(
                        JobSeekerContactRequestsController
                            .RespondToContactRequest));

        Assert.NotNull(method);

        var patch =
            method!
                .GetCustomAttributes(
                    typeof(HttpPatchAttribute),
                    inherit: true)
                .Cast<HttpPatchAttribute>()
                .Single();

        Assert.Equal(
            "{contactRequestId:guid}/status",
            patch.Template);
    }

    [Fact]
    public async Task JobSeeker_respond_returns_200_and_forwards_current_user()
    {
        var jobSeekerUserId = Guid.NewGuid();
        var contactRequestId = Guid.NewGuid();

        var request =
            new RespondContactRequestRequest(
                ContactRequestStatus.Accepted,
                new byte[] { 1 });

        var expected =
            new ContactRequestDto(
                contactRequestId,
                Guid.NewGuid(),
                ContactRequestStatus.Accepted,
                Utc(12),
                Utc(16),
                new byte[] { 2 });

        var service =
            new FakeContactRequestService
            {
                RespondResult =
                    ContactRequestWriteResult
                        .Success(expected)
            };

        var controller =
            new JobSeekerContactRequestsController(
                service,
                new FakeCurrentUser(
                    jobSeekerUserId,
                    RoleNames.JobSeeker));

        var action =
            await controller.RespondToContactRequest(
                contactRequestId,
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        Assert.Same(
            expected,
            ok.Value);

        Assert.Equal(
            jobSeekerUserId,
            service.LastJobSeekerUserId);

        Assert.Equal(
            contactRequestId,
            service.LastContactRequestId);

        Assert.Same(
            request,
            service.LastResponseRequest);
    }

    [Fact]
    public async Task JobSeeker_respond_returns_401_for_invalid_current_user()
    {
        var controller =
            new JobSeekerContactRequestsController(
                new FakeContactRequestService(),
                new FakeCurrentUser(
                    null,
                    RoleNames.JobSeeker));

        var action =
            await controller.RespondToContactRequest(
                Guid.NewGuid(),
                new RespondContactRequestRequest(
                    ContactRequestStatus.Accepted,
                    new byte[] { 1 }),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);
    }

    [Theory]
    [InlineData(
        ContactRequestWriteFailureReason.InvalidInput,
        StatusCodes.Status400BadRequest)]
    [InlineData(
        ContactRequestWriteFailureReason.NotFound,
        StatusCodes.Status404NotFound)]
    [InlineData(
        ContactRequestWriteFailureReason.ParticipantInactive,
        StatusCodes.Status403Forbidden)]
    [InlineData(
        ContactRequestWriteFailureReason.InvalidTransition,
        StatusCodes.Status409Conflict)]
    [InlineData(
        ContactRequestWriteFailureReason.ConcurrencyConflict,
        StatusCodes.Status409Conflict)]
    [InlineData(
        ContactRequestWriteFailureReason.PersistenceFailed,
        StatusCodes.Status500InternalServerError)]
    public async Task JobSeeker_respond_maps_failure_to_expected_status(
        ContactRequestWriteFailureReason failureReason,
        int expectedStatus)
    {
        var service =
            new FakeContactRequestService
            {
                RespondResult =
                    ContactRequestWriteResult
                        .Failure(failureReason)
            };

        var controller =
            new JobSeekerContactRequestsController(
                service,
                new FakeCurrentUser(
                    Guid.NewGuid(),
                    RoleNames.JobSeeker));

        var action =
            await controller.RespondToContactRequest(
                Guid.NewGuid(),
                new RespondContactRequestRequest(
                    ContactRequestStatus.Accepted,
                    new byte[] { 1 }),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            expectedStatus,
            problem.StatusCode);
    }

    private static ContactRequestDto CreateContactRequestDto(
        Guid applicationId,
        ContactRequestStatus status)
    {
        return new ContactRequestDto(
            Guid.NewGuid(),
            applicationId,
            status,
            Utc(12),
            null,
            new byte[] { 1 });
    }

    private static DateTime Utc(
        int hour) =>
        new(
            2026,
            9,
            12,
            hour,
            0,
            0,
            DateTimeKind.Utc);

    private sealed class FakeContactRequestService
        : IContactRequestService
    {
        public ContactRequestWriteResult CreateResult { get; set; } =
            ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.InvalidInput);

        public ContactRequestWriteResult RespondResult { get; set; } =
            ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.InvalidInput);

        public Guid? LastEmployerUserId { get; private set; }

        public Guid? LastApplicationId { get; private set; }

        public Guid? LastJobSeekerUserId { get; private set; }

        public Guid? LastContactRequestId { get; private set; }

        public RespondContactRequestRequest?
            LastResponseRequest { get; private set; }

        public Task<ContactRequestWriteResult>
            CreateForOwnApplicationAsync(
                Guid employerUserId,
                Guid jobApplicationId,
                CancellationToken cancellationToken = default)
        {
            LastEmployerUserId =
                employerUserId;

            LastApplicationId =
                jobApplicationId;

            return Task.FromResult(
                CreateResult);
        }

        public Task<ContactRequestWriteResult>
            RespondToOwnContactRequestAsync(
                Guid jobSeekerUserId,
                Guid contactRequestId,
                RespondContactRequestRequest request,
                CancellationToken cancellationToken = default)
        {
            LastJobSeekerUserId =
                jobSeekerUserId;

            LastContactRequestId =
                contactRequestId;

            LastResponseRequest =
                request;

            return Task.FromResult(
                RespondResult);
        }
    }

    private sealed class FakeCurrentUser
        : ICurrentUser
    {
        public FakeCurrentUser(
            Guid? userId,
            string role)
        {
            UserId = userId;
            Role = role;
        }

        public bool IsAuthenticated =>
            UserId.HasValue;

        public Guid? UserId { get; }

        public string? Role { get; }

        public string? Email =>
            "test@example.com";
    }
}
