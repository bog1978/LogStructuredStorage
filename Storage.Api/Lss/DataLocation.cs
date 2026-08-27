namespace Storage.Api.Lss;

public sealed record DataLocation(
    string BucketName,
    int PartNumber,
    long Offset);