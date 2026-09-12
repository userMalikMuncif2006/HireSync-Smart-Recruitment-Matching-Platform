using System.Reflection;
using HireSync.Api.Controllers;
using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.JobSeeker;
using HireSync.Application.Security;
using HireSync.Application.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace HireSync.Api.IntegrationTests;

public sealed class JobSeekerCvControllerContractTests
{
    [Fact]
    public void Controller_requires_JobSeeker_role()
    {
        var attribute =
            typeof(JobSeekerCvController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute),
                    inherit:
                        true)
                .Cast<AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            RoleNames.JobSeeker,
            attribute.Roles);
    }

    [Fact]
    public void Controller_exposes_expected_cv_routes()
    {
        var route =
            typeof(JobSeekerCvController)
                .GetCustomAttributes(
                    typeof(RouteAttribute),
                    inherit:
                        true)
                .Cast<RouteAttribute>()
                .Single();

        Assert.Equal(
            "api/v1/job-seeker/cv",
            route.Template);

        var metadataGet =
            typeof(JobSeekerCvController)
                .GetMethod(
                    nameof(
                        JobSeekerCvController
                            .GetOwnCv))!;

        var upload =
            typeof(JobSeekerCvController)
                .GetMethod(
                    nameof(
                        JobSeekerCvController
                            .UploadOwnCv))!;

        var download =
            typeof(JobSeekerCvController)
                .GetMethod(
                    nameof(
                        JobSeekerCvController
                            .DownloadOwnCv))!;

        Assert.Null(
            metadataGet
                .GetCustomAttribute<HttpGetAttribute>()!
                .Template);

        Assert.Null(
            upload
                .GetCustomAttribute<HttpPostAttribute>()!
                .Template);

        Assert.Equal(
            "file",
            download
                .GetCustomAttribute<HttpGetAttribute>()!
                .Template);
    }

    [Fact]
    public void Upload_parameter_binds_exact_file_field()
    {
        var method =
            typeof(JobSeekerCvController)
                .GetMethod(
                    nameof(
                        JobSeekerCvController
                            .UploadOwnCv))!;

        var fileParameter =
            method
                .GetParameters()
                .Single(
                    parameter =>
                        parameter.ParameterType ==
                        typeof(IFormFile));

        var attribute =
            fileParameter
                .GetCustomAttribute<FromFormAttribute>();

        Assert.NotNull(
            attribute);

        Assert.Equal(
            "file",
            attribute!.Name);
    }

    [Fact]
    public async Task GetOwnCv_returns_200_with_public_metadata()
    {
        var metadata =
            CreateMetadata();

        var service =
            new FakeJobSeekerCvService
            {
                Metadata =
                    metadata
            };

        var controller =
            CreateController(
                service);

        var result =
            await controller.GetOwnCv(
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<JobSeekerCvDto>(
                ok.Value);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        Assert.Equal(
            metadata.OriginalFileName,
            response.OriginalFileName);

        Assert.Equal(
            1,
            service.GetCallCount);
    }

    [Fact]
    public async Task GetOwnCv_returns_404_when_metadata_missing()
    {
        var service =
            new FakeJobSeekerCvService();

        var controller =
            CreateController(
                service);

        var result =
            await controller.GetOwnCv(
                CancellationToken.None);

        AssertProblemStatus(
            result,
            StatusCodes.Status404NotFound);

        Assert.Equal(
            1,
            service.GetCallCount);
    }

    [Fact]
    public async Task Upload_requires_multipart_file()
    {
        var service =
            new FakeJobSeekerCvService();

        var controller =
            CreateController(
                service);

        var result =
            await controller.UploadOwnCv(
                null,
                CancellationToken.None);

        AssertProblemStatus(
            result,
            StatusCodes.Status400BadRequest);

        Assert.Equal(
            0,
            service.UploadCallCount);
    }

    [Fact]
    public async Task Upload_rejects_wrong_file_field_name()
    {
        var service =
            new FakeJobSeekerCvService();

        var controller =
            CreateController(
                service);

        var file =
            CreatePdfFormFile(
                fieldName:
                    "cv");

        SetMultipartForm(
            controller,
            new[]
            {
                file
            });

        var result =
            await controller.UploadOwnCv(
                file,
                CancellationToken.None);

        AssertProblemStatus(
            result,
            StatusCodes.Status400BadRequest);

        Assert.Equal(
            0,
            service.UploadCallCount);
    }

    [Fact]
    public async Task Upload_rejects_additional_form_fields()
    {
        var service =
            new FakeJobSeekerCvService();

        var controller =
            CreateController(
                service);

        var file =
            CreatePdfFormFile();

        SetMultipartForm(
            controller,
            new[]
            {
                file
            },
            new Dictionary<string, StringValues>
            {
                ["unexpected"] =
                    "value"
            });

        var result =
            await controller.UploadOwnCv(
                file,
                CancellationToken.None);

        AssertProblemStatus(
            result,
            StatusCodes.Status400BadRequest);

        Assert.Equal(
            0,
            service.UploadCallCount);
    }

    [Fact]
    public async Task Upload_returns_413_before_service_for_oversized_file()
    {
        var service =
            new FakeJobSeekerCvService();

        var controller =
            CreateController(
                service);

        var file =
            CreatePdfFormFile(
                declaredLength:
                    CvValidationPolicy
                        .MaxCvBytes +
                    1);

        SetMultipartForm(
            controller,
            new[]
            {
                file
            });

        var result =
            await controller.UploadOwnCv(
                file,
                CancellationToken.None);

        AssertProblemStatus(
            result,
            StatusCodes.Status413PayloadTooLarge);

        Assert.Equal(
            0,
            service.UploadCallCount);
    }

    [Fact]
    public async Task Upload_returns_200_and_forwards_file_metadata()
    {
        var metadata =
            CreateMetadata();

        var service =
            new FakeJobSeekerCvService
            {
                UploadResult =
                    CvUploadResult.Success(
                        metadata)
            };

        var controller =
            CreateController(
                service);

        var file =
            CreatePdfFormFile();

        SetMultipartForm(
            controller,
            new[]
            {
                file
            });

        var result =
            await controller.UploadOwnCv(
                file,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        Assert.Equal(
            1,
            service.UploadCallCount);

        Assert.NotNull(
            service.LastUploadRequest);

        Assert.Equal(
            "resume.pdf",
            service.LastUploadRequest!
                .OriginalFileName);

        Assert.Equal(
            "application/pdf",
            service.LastUploadRequest
                .DeclaredContentType);
    }

    [Fact]
    public async Task Upload_returns_400_for_invalid_file_type()
    {
        var result =
            await ExecuteUploadFailureAsync(
                CvOperationFailureReason
                    .InvalidFileType);

        AssertProblemStatus(
            result,
            StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Upload_returns_413_for_service_size_limit()
    {
        var result =
            await ExecuteUploadFailureAsync(
                CvOperationFailureReason
                    .FileTooLarge);

        AssertProblemStatus(
            result,
            StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task Upload_returns_415_for_malformed_file()
    {
        var result =
            await ExecuteUploadFailureAsync(
                CvOperationFailureReason
                    .MalformedFile);

        AssertProblemStatus(
            result,
            StatusCodes.Status415UnsupportedMediaType);
    }

    [Fact]
    public async Task Upload_returns_503_for_storage_unavailable()
    {
        var result =
            await ExecuteUploadFailureAsync(
                CvOperationFailureReason
                    .StorageUnavailable);

        AssertProblemStatus(
            result,
            StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Upload_returns_404_when_profile_missing()
    {
        var result =
            await ExecuteUploadFailureAsync(
                CvOperationFailureReason
                    .ProfileNotFound);

        AssertProblemStatus(
            result,
            StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Download_returns_attachment_contract_and_private_no_cache_headers()
    {
        var content =
            new MemoryStream(
                "%PDF-1.7 synthetic"u8
                    .ToArray());

        var service =
            new FakeJobSeekerCvService
            {
                DownloadResult =
                    CvDownloadResult.Success(
                        content,
                        "resume.pdf",
                        "application/pdf")
            };

        var controller =
            CreateController(
                service);

        var result =
            await controller.DownloadOwnCv(
                CancellationToken.None);

        var file =
            Assert.IsType<FileStreamResult>(
                result);

        Assert.Equal(
            "application/pdf",
            file.ContentType);

        Assert.Equal(
            "resume.pdf",
            file.FileDownloadName);

        Assert.False(
            file.EnableRangeProcessing);

        Assert.Equal(
            "private, no-store, no-cache",
            controller.Response.Headers[
                "Cache-Control"]
                .ToString());

        Assert.Equal(
            "no-cache",
            controller.Response.Headers[
                "Pragma"]
                .ToString());

        Assert.Equal(
            "0",
            controller.Response.Headers[
                "Expires"]
                .ToString());

        Assert.Equal(
            1,
            service.DownloadCallCount);

        await file.FileStream.DisposeAsync();
    }

    [Fact]
    public async Task Download_returns_404_when_cv_missing()
    {
        var service =
            new FakeJobSeekerCvService
            {
                DownloadResult =
                    CvDownloadResult.Failure(
                        CvDownloadFailureReason
                            .NotFound)
            };

        var controller =
            CreateController(
                service);

        var result =
            await controller.DownloadOwnCv(
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            problem.StatusCode);
    }

    [Fact]
    public async Task Download_returns_503_when_storage_unavailable()
    {
        var service =
            new FakeJobSeekerCvService
            {
                DownloadResult =
                    CvDownloadResult.Failure(
                        CvDownloadFailureReason
                            .StorageUnavailable)
            };

        var controller =
            CreateController(
                service);

        var result =
            await controller.DownloadOwnCv(
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result);

        Assert.Equal(
            StatusCodes.Status503ServiceUnavailable,
            problem.StatusCode);
    }

    private static async Task<ActionResult<JobSeekerCvDto>>
        ExecuteUploadFailureAsync(
            CvOperationFailureReason failureReason)
    {
        var service =
            new FakeJobSeekerCvService
            {
                UploadResult =
                    CvUploadResult.Failure(
                        failureReason)
            };

        var controller =
            CreateController(
                service);

        var file =
            CreatePdfFormFile();

        SetMultipartForm(
            controller,
            new[]
            {
                file
            });

        return await controller.UploadOwnCv(
            file,
            CancellationToken.None);
    }

    private static void AssertProblemStatus(
        ActionResult<JobSeekerCvDto> result,
        int expectedStatusCode)
    {
        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            expectedStatusCode,
            problem.StatusCode);
    }

    private static JobSeekerCvController
        CreateController(
            IJobSeekerCvService service)
    {
        var controller =
            new JobSeekerCvController(
                service);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        return controller;
    }

    private static void SetMultipartForm(
        JobSeekerCvController controller,
        IEnumerable<IFormFile> files,
        Dictionary<string, StringValues>? fields =
            null)
    {
        var fileCollection =
            new FormFileCollection();

        foreach (var file in files)
        {
            fileCollection.Add(
                file);
        }

        controller.Request.ContentType =
            "multipart/form-data; boundary=HireSyncTestBoundary";

        controller.Request.Form =
            new FormCollection(
                fields ??
                    new Dictionary<string, StringValues>(),
                fileCollection);
    }

    private static IFormFile CreatePdfFormFile(
        string fieldName = "file",
        long? declaredLength = null)
    {
        var bytes =
            "%PDF-1.7 synthetic"u8
                .ToArray();

        var stream =
            new MemoryStream(
                bytes);

        var file =
            new FormFile(
                stream,
                0,
                declaredLength ??
                    bytes.LongLength,
                fieldName,
                "resume.pdf")
            {
                Headers =
                    new HeaderDictionary(),
                ContentType =
                    "application/pdf"
            };

        return file;
    }

    private static JobSeekerCvDto
        CreateMetadata()
    {
        return new JobSeekerCvDto(
            "resume.pdf",
            ".pdf",
            "application/pdf",
            18,
            new DateTime(
                2026,
                9,
                12,
                8,
                0,
                0,
                DateTimeKind.Utc));
    }

    private sealed class FakeJobSeekerCvService
        : IJobSeekerCvService
    {
        public JobSeekerCvDto? Metadata
        {
            get;
            set;
        }

        public CvUploadResult UploadResult
        {
            get;
            set;
        } =
            CvUploadResult.Failure(
                CvOperationFailureReason
                    .PersistenceFailed);

        public CvDownloadResult DownloadResult
        {
            get;
            set;
        } =
            CvDownloadResult.Failure(
                CvDownloadFailureReason
                    .NotFound);

        public CvUploadRequest? LastUploadRequest
        {
            get;
            private set;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public int UploadCallCount
        {
            get;
            private set;
        }

        public int DownloadCallCount
        {
            get;
            private set;
        }

        public Task<JobSeekerCvDto?>
            GetOwnCvAsync(
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            GetCallCount++;

            return Task.FromResult(
                Metadata);
        }

        public Task<CvDownloadResult>
            DownloadOwnCvAsync(
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            DownloadCallCount++;

            return Task.FromResult(
                DownloadResult);
        }

        public Task<CvUploadResult>
            UploadOrReplaceOwnCvAsync(
                CvUploadRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            UploadCallCount++;

            LastUploadRequest =
                request;

            return Task.FromResult(
                UploadResult);
        }
    }
}
