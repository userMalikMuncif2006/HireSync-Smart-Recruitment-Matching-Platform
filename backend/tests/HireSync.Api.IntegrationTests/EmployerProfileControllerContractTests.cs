using System.Security.Claims;
using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Employer;
using HireSync.Application.Interfaces.Employer;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public class EmployerProfileControllerContractTests
{
    [Fact]
    public void Controller_requires_Employer_role()
    {
        var attribute = typeof(EmployerProfileController)
            .GetCustomAttributes(
                typeof(AuthorizeAttribute),
                inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            RoleNames.Employer,
            attribute.Roles);
    }

    [Fact]
    public async Task GetOwnProfile_returns_200_for_existing_profile()
    {
        var employerUserId = Guid.NewGuid();
        var profile = CreateProfile();

        var service = new FakeEmployerProfileService
        {
            Profile = profile
        };

        var controller =
            CreateController(service, employerUserId);

        var result = await controller.GetOwnProfile(
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response =
            Assert.IsType<EmployerProfileDto>(ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(profile.Id, response.Id);
        Assert.Equal(
            "employer@example.com",
            response.BusinessEmail);
        Assert.Equal(
            EmployerVerificationStatus.Approved,
            response.EmployerVerificationStatus);
        Assert.True(response.IsProfileComplete);
        Assert.True(response.IsVacancyReady);

        Assert.Equal(
            employerUserId,
            service.LastEmployerUserId);
    }

    [Fact]
    public async Task GetOwnProfile_returns_404_when_profile_missing()
    {
        var service = new FakeEmployerProfileService
        {
            Profile = null
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result = await controller.GetOwnProfile(
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(404, problem.StatusCode);
    }

    [Fact]
    public async Task GetOwnProfile_returns_401_when_sub_claim_missing()
    {
        var service = new FakeEmployerProfileService();

        var controller =
            CreateControllerWithoutUser(service);

        var result = await controller.GetOwnProfile(
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(401, problem.StatusCode);
        Assert.Null(service.LastEmployerUserId);
    }

    [Fact]
    public async Task GetOwnProfile_returns_401_when_sub_claim_invalid()
    {
        var service = new FakeEmployerProfileService();

        var controller =
            CreateControllerWithSub(
                service,
                "not-a-guid");

        var result = await controller.GetOwnProfile(
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(401, problem.StatusCode);
        Assert.Null(service.LastEmployerUserId);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_200_for_success()
    {
        var employerUserId = Guid.NewGuid();
        var profile = CreateProfile();
        var request = CreateUpdateRequest();

        var service = new FakeEmployerProfileService
        {
            UpdateResult =
                EmployerProfileUpdateResult.Success(profile)
        };

        var controller =
            CreateController(service, employerUserId);

        var result = await controller.UpdateOwnProfile(
            request,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response =
            Assert.IsType<EmployerProfileDto>(ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(profile.Id, response.Id);

        Assert.Equal(
            employerUserId,
            service.LastEmployerUserId);

        Assert.Same(
            request,
            service.LastUpdateRequest);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_400_for_invalid_input()
    {
        var service = new FakeEmployerProfileService
        {
            UpdateResult =
                EmployerProfileUpdateResult.Failure(
                    EmployerProfileUpdateFailureReason.InvalidInput)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result = await controller.UpdateOwnProfile(
            CreateUpdateRequest(),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_404_when_profile_missing()
    {
        var service = new FakeEmployerProfileService
        {
            UpdateResult =
                EmployerProfileUpdateResult.Failure(
                    EmployerProfileUpdateFailureReason.NotFound)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result = await controller.UpdateOwnProfile(
            CreateUpdateRequest(),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(404, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_409_for_duplicate_brn()
    {
        var service = new FakeEmployerProfileService
        {
            UpdateResult =
                EmployerProfileUpdateResult.Failure(
                    EmployerProfileUpdateFailureReason
                        .DuplicateBusinessRegistrationNumber)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result = await controller.UpdateOwnProfile(
            CreateUpdateRequest(),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_409_when_reverification_required()
    {
        var service = new FakeEmployerProfileService
        {
            UpdateResult =
                EmployerProfileUpdateResult.Failure(
                    EmployerProfileUpdateFailureReason
                        .VerificationResetRequired)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result = await controller.UpdateOwnProfile(
            CreateUpdateRequest(),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_500_when_persistence_fails()
    {
        var service = new FakeEmployerProfileService
        {
            UpdateResult =
                EmployerProfileUpdateResult.Failure(
                    EmployerProfileUpdateFailureReason.PersistenceFailed)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result = await controller.UpdateOwnProfile(
            CreateUpdateRequest(),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(500, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_401_when_sub_claim_missing()
    {
        var service = new FakeEmployerProfileService();

        var controller =
            CreateControllerWithoutUser(service);

        var result = await controller.UpdateOwnProfile(
            CreateUpdateRequest(),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(401, problem.StatusCode);
        Assert.Null(service.LastEmployerUserId);
    }

    private static EmployerProfileController CreateController(
        FakeEmployerProfileService service,
        Guid employerUserId)
    {
        return CreateControllerWithSub(
            service,
            employerUserId.ToString());
    }

    private static EmployerProfileController CreateControllerWithSub(
        FakeEmployerProfileService service,
        string sub)
    {
        var controller =
            new EmployerProfileController(service);

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", sub),
                new Claim("role", RoleNames.Employer)
            },
            authenticationType: "Test");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(identity)
                    }
            };

        return controller;
    }

    private static EmployerProfileController CreateControllerWithoutUser(
        FakeEmployerProfileService service)
    {
        var controller =
            new EmployerProfileController(service);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        return controller;
    }

    private static EmployerProfileDto CreateProfile()
    {
        return new EmployerProfileDto(
            Guid.NewGuid(),
            "Acme Holdings",
            "A complete Employer company profile.",
            "Colombo",
            "Test Contact",
            "HR Manager",
            "PV 12345",
            "0771234567",
            "https://example.com",
            "employer@example.com",
            EmployerVerificationStatus.Approved,
            true,
            true);
    }

    private static UpdateEmployerProfileRequest
        CreateUpdateRequest()
    {
        return new UpdateEmployerProfileRequest(
            "Acme Holdings",
            "A complete Employer company profile.",
            "Colombo",
            "Test Contact",
            "HR Manager",
            "PV 12345",
            "0771234567",
            "https://example.com");
    }

    private sealed class FakeEmployerProfileService
        : IEmployerProfileService
    {
        public EmployerProfileDto? Profile { get; set; }

        public EmployerProfileUpdateResult UpdateResult { get; set; } =
            EmployerProfileUpdateResult.Failure(
                EmployerProfileUpdateFailureReason.PersistenceFailed);

        public Guid? LastEmployerUserId { get; private set; }

        public UpdateEmployerProfileRequest?
            LastUpdateRequest { get; private set; }

        public Task<EmployerProfileDto?> GetOwnProfileAsync(
            Guid employerUserId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastEmployerUserId = employerUserId;

            return Task.FromResult(Profile);
        }

        public Task<EmployerProfileUpdateResult> UpdateOwnProfileAsync(
            Guid employerUserId,
            UpdateEmployerProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastEmployerUserId = employerUserId;
            LastUpdateRequest = request;

            return Task.FromResult(UpdateResult);
        }
    }
}