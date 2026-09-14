using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Services;
using HireSync.Domain.Enums;

namespace HireSync.Application.Tests;

public sealed class EmployerRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_delegates_valid_request()
    {
        var expected =
            EmployerRegistrationResult.Success(
                new RegisterEmployerResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "employer@example.com",
                    "Employer",
                    EmployerVerificationStatus.Pending));

        var provisioner =
            new FakeEmployerRegistrationProvisioner(expected);

        var service =
            new EmployerRegistrationService(provisioner);

        var request = CreateValidRequest();

        var result =
            await service.RegisterAsync(request);

        Assert.True(result.Succeeded);
        Assert.Same(expected, result);
        Assert.Equal(1, provisioner.CallCount);
        Assert.Same(request, provisioner.LastRequest);
    }

    [Fact]
    public async Task RegisterAsync_rejects_invalid_input_without_provisioning()
    {
        var provisioner =
            new FakeEmployerRegistrationProvisioner(
                EmployerRegistrationResult.Failure(
                    EmployerRegistrationFailureReason.PersistenceFailed));

        var service =
            new EmployerRegistrationService(provisioner);

        var request =
            CreateValidRequest() with
            {
                CompanyName = "A"
            };

        var result =
            await service.RegisterAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(
            EmployerRegistrationFailureReason.InvalidInput,
            result.FailureReason);

        Assert.Equal(0, provisioner.CallCount);
    }

    [Fact]
    public async Task RegisterAsync_preserves_provisioning_failure()
    {
        var expected =
            EmployerRegistrationResult.Failure(
                EmployerRegistrationFailureReason
                    .DuplicateBusinessRegistrationNumber);

        var provisioner =
            new FakeEmployerRegistrationProvisioner(expected);

        var service =
            new EmployerRegistrationService(provisioner);

        var result =
            await service.RegisterAsync(
                CreateValidRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            EmployerRegistrationFailureReason
                .DuplicateBusinessRegistrationNumber,
            result.FailureReason);

        Assert.Same(expected, result);
    }

    private static RegisterEmployerRequest CreateValidRequest()
    {
        return new RegisterEmployerRequest(
            "employer@example.com",
            "ValidPassword123!",
            "Example Holdings",
            "A complete Employer organisation profile description.",
            "Colombo",
            "Test Contact",
            "HR Manager",
            "PV 12345",
            "0771234567",
            "https://example.com");
    }

    private sealed class FakeEmployerRegistrationProvisioner(
        EmployerRegistrationResult result)
        : IEmployerRegistrationProvisioner
    {
        public int CallCount { get; private set; }

        public RegisterEmployerRequest? LastRequest { get; private set; }

        public Task<EmployerRegistrationResult> ProvisionAsync(
            RegisterEmployerRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;

            return Task.FromResult(result);
        }
    }
}
