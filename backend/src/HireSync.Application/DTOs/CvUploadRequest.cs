namespace HireSync.Application.DTOs;

public sealed record CvUploadRequest(
    Stream Content,
    string OriginalFileName,
    string DeclaredContentType);
