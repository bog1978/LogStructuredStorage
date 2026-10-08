namespace Storage.Api.Lss.Model;

internal sealed record DataLocation(
    string BucketName,
    int PartNumber,
    int FileIndex);
