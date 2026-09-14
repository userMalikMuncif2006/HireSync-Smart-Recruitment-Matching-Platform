namespace HireSync.Application.DTOs;

public sealed record JobSeekerCvDto(
    string OriginalFileName,
    string Extension,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAtUtc);
