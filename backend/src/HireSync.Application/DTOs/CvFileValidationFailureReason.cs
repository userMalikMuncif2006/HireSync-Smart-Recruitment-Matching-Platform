namespace HireSync.Application.DTOs;

public enum CvFileValidationFailureReason
{
    None = 0,
    InvalidOriginalFileName = 1,
    EmptyFile = 2,
    FileTooLarge = 3,
    UnsupportedExtension = 4,
    UnsupportedContentType = 5,
    UnreadableContent = 6,
    InvalidPdfSignature = 7,
    InvalidDocxPackage = 8,
    UnsafeDocxEntryPath = 9,
    DocxEntryCountExceeded = 10,
    DocxEntrySizeExceeded = 11,
    DocxTotalSizeExceeded = 12,
    DocxCompressionRatioExceeded = 13
}
