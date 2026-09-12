using HireSync.Domain.Entities;

namespace HireSync.Application.Validation;

public static class CvValidationPolicy
{
    public const long MaxCvBytes =
        CvDocument.MaxSizeBytes;

    public const int MaxOriginalFileNameLength =
        CvDocument.MaxOriginalFileNameLength;

    public const int MaxDocxEntryCount =
        1_000;

    public const long MaxDocxEntryUncompressedBytes =
        10_000_000;

    public const long MaxDocxTotalUncompressedBytes =
        25_000_000;

    public const double MaxDocxCompressionRatio =
        100d;
}
