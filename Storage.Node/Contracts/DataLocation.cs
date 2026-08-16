namespace Storage.Node;

public sealed record DataLocation(
    string BucketName,
    int PartNumber,
    long Offset);