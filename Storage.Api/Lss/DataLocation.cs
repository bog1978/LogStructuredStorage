namespace Storage.Api.Lss;

internal sealed record DataLocation(
    string BucketName,
    int PartNumber,
    long Offset);